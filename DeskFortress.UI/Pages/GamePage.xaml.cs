using DeskFortress.Core.Entities;
using DeskFortress.Core.Simulation;
using DeskFortress.Core.World;
using DeskFortress.UI.Audio;
using DeskFortress.UI.Controls;
using DeskFortress.UI.CoreIntegration;
using DeskFortress.UI.Game;
using DeskFortress.UI.Rendering;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Layouts;

namespace DeskFortress.UI.Pages;

public partial class GamePage : ContentPage
{
    private enum OverlayMode
    {
        None,
        Intro,
        Pause,
        GameOver
    }

    private readonly GameSessionManager _sessions;
    private readonly GameLoopService _loop;
    private readonly Renderer _renderer;
    private readonly AudioService _audio;
    private readonly WorldViewportService _viewportService;
    private readonly CoreBootstrapper _core;

    private readonly Dictionary<Guid, Image> _sprites = new();
    private readonly Random _random = new();

    private IDispatcherTimer? _timer;
    private bool _started;
    private bool _worldConfigured;
    private bool _overlayVisible;
    private bool _gameOverOverlayShown;
    private OverlayMode _overlayMode;

    // ── Pointer-based throw state ─────────────────────────────────────────────
    //
    // We use PointerGestureRecognizer on the overlay instead of
    // PanGestureRecognizer so we always get absolute screen coordinates —
    // no TotalX/TotalY ambiguity between platforms.
    private Point _throwPressStart;    // overlay-relative coords at press-down
    private Point _throwPressCurrent;  // latest pointer position during drag
    private bool  _throwPointerDown;   // any pointer currently pressed
    private bool  _throwModeActive;    // press started within the ball hit-zone

    // Rolling sample buffer: stores (dx,dy) relative to press-start for
    // instantaneous flick-speed estimation at release.
    private record struct FlickSample(float Dx, float Dy, long TickMs);
    private readonly FlickSample[] _flickBuffer = new FlickSample[8];
    private int _flickHead;
    private int _flickCount;
    private float _touchThrowDx;
    private float _touchThrowDy;

    // Prevent overlapping throws while a fake flight animation is running.
    private bool _throwAnimationActive;

    // Trajectory-arc preview (ThrowTrajectoryDrawable). Wired to TrajectoryView.
    private readonly ThrowTrajectoryDrawable _trajectoryDrawable = new();

    // ── Camera pan state ─────────────────────────────────────────────────────
    private double _camPanLastX;
    private bool   _isCameraPanning;
    private double _panSliderInputNormalized;

    // ── Debug ────────────────────────────────────────────────────────────────
    private int _frameCount;

    private const double BackgroundPixelWidth = 314.560;
    private const double BackgroundPixelHeight = 115.200;
    private const double BackgroundAspectRatio = BackgroundPixelWidth / BackgroundPixelHeight;
    private const double PanSliderTravel = 28.0;
    private const double PanSliderDeadZone = 0.08;
    private const float ThrowControlFollowFactor = 0.20f;

    public GamePage(
        GameSessionManager sessions,
        GameLoopService loop,
        Renderer renderer,
        AudioService audio,
        WorldViewportService viewportService,
        CoreBootstrapper core)
    {
        InitializeComponent();

        _sessions = sessions;
        _loop = loop;
        _renderer = renderer;
        _audio = audio;
        _viewportService = viewportService;
        _core = core;

        // Wire the trajectory drawable to the GraphicsView declared in XAML.
        TrajectoryView.Drawable = _trajectoryDrawable;
    }

    #region Lifecycle

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (_started)
            return;

        _started = true;
        _overlayVisible = false;
        _gameOverOverlayShown = false;
        _overlayMode = OverlayMode.None;
        GameOverlay.IsVisible = false;
        _sessions.StartNewGame();

        if (_sessions.Current is { } startedSession)
        {
            startedSession.IsRunning = false;
        }

        _audio.PlayMusic("game");

        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(16);
        _timer.Tick += OnTick;
        _timer.Start();

        System.Diagnostics.Debug.WriteLine("=== GAME STARTED ===");

        var session = _sessions.Current;
        if (session != null)
        {
            System.Diagnostics.Debug.WriteLine(
                $"World BackDepthY={session.World.Map.BackDepthY:F2}, FrontDepthY={session.World.Map.FrontDepthY:F2}");
        }

        ShowOverlay(OverlayMode.Intro);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        if (_timer != null)
        {
            _timer.Stop();
            _timer.Tick -= OnTick;
            _timer = null;
        }

        _audio.StopMusic();
        _sessions.Stop();
        _sprites.Clear();
        WorldLayer.Children.Clear();
        _started = false;
        _worldConfigured = false;

        // Reset throw / pointer state.
        _throwPointerDown  = false;
        _throwModeActive   = false;
        _isCameraPanning   = false;
        _panSliderInputNormalized = 0;
        _throwAnimationActive = false;
        _overlayVisible = false;
        _gameOverOverlayShown = false;
        _overlayMode = OverlayMode.None;
        _flickHead  = 0;
        _flickCount = 0;
        _touchThrowDx = 0f;
        _touchThrowDy = 0f;
        ThrowFlyingBall.IsVisible = false;
        ThrowFlyingBall.TranslationX = 0;
        ThrowFlyingBall.TranslationY = 0;
        ThrowOriginBall.TranslationX = 0;
        ThrowOriginBall.TranslationY = 0;
        PanSliderThumb.TranslationX = 0;
        GameOverlay.IsVisible = false;
        HideTrajectoryPreview();
        ThrowOriginBall.IsVisible = true;
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        if (width <= 0 || height <= 0)
            return;

        ConfigureWorld(width, height);
    }

    #endregion

    #region World Configuration

    private void ConfigureWorld(double viewportWidth, double viewportHeight)
    {
        _viewportService.Configure(viewportWidth, viewportHeight, BackgroundAspectRatio);

        AbsoluteLayout.SetLayoutFlags(BackgroundImage, AbsoluteLayoutFlags.None);
        AbsoluteLayout.SetLayoutBounds(
            BackgroundImage,
            new Rect(0, 0, _viewportService.WorldWidth, _viewportService.WorldHeight));

        AbsoluteLayout.SetLayoutFlags(WorldLayer, AbsoluteLayoutFlags.None);
        AbsoluteLayout.SetLayoutBounds(
            WorldLayer,
            new Rect(0, 0, _viewportService.WorldWidth, _viewportService.WorldHeight));

        AbsoluteLayout.SetLayoutFlags(WorldRoot, AbsoluteLayoutFlags.None);
        AbsoluteLayout.SetLayoutBounds(
            WorldRoot,
            new Rect(0, 0, _viewportService.WorldWidth, _viewportService.WorldHeight));

        ApplyCamera();
        _worldConfigured = true;

        System.Diagnostics.Debug.WriteLine(
            $"World configured: viewport={viewportWidth}x{viewportHeight}, world={_viewportService.WorldWidth}x{_viewportService.WorldHeight}");
    }

    #endregion

    #region Game Loop

    private void OnTick(object? sender, EventArgs e)
    {
        var session = _sessions.Current;
        if (session == null || !_worldConfigured)
            return;

        _frameCount++;

        ApplyContinuousPanFromSlider(1f / 60f);
        _loop.Tick(session, 1f / 60f);
        if (session.IsRunning)
        {
            HandleSpawnRequests(session);
        }
        RenderFrame();
        UpdateHUD(session);

        if (session.World.IsGameOver && !_gameOverOverlayShown)
        {
            HandleGameOver();
        }
    }

    private void HandleSpawnRequests(GameSession session)
    {
        foreach (var evt in session.World.Events.Where(e => e.Type == "spawn_requested"))
        {
            var coworker = _core.CreateRandomCoworker(_random);
            session.World.SpawnCoworker(coworker);

            System.Diagnostics.Debug.WriteLine(
                $"Coworker spawned at ({coworker.X:F2}, {coworker.Y:F2}), depth={coworker.Depth:F2}, scale={coworker.Scale:F2}");
        }
    }

    private void UpdateHUD(GameSession session)
    {
        var world = session.World;
        ScoreLabel.Text =
            $"Score: {world.Score} | Wave: {world.WaveSpawnManager.CurrentWave} | " +
            $"Crowding: {world.CoworkersReachedFront}/15 | Throws: {session.Throws} | Projectiles: {world.Projectiles.Count}";
    }

    private void HandleGameOver()
    {
        var session = _sessions.Current;
        if (session != null)
        {
            session.IsRunning = false;
        }

        _audio.StopMusic();
        _gameOverOverlayShown = true;
        ShowOverlay(OverlayMode.GameOver);
    }

    #endregion

    #region Rendering

    private void RenderFrame()
    {
        var frame = _renderer.LastFrame;
        var visibleIds = new HashSet<Guid>();

        foreach (var item in frame)
        {
            var dto = item.Dto;
            visibleIds.Add(dto.Id);

            if (dto.ShouldBeOccluded)
                continue;

            if (!_sprites.TryGetValue(dto.Id, out var image))
            {
                image = new Image
                {
                    Source = ImageSource.FromFile(item.ImagePath),
                    Aspect = Aspect.AspectFit,
                    InputTransparent = true
                };

                _sprites[dto.Id] = image;
                WorldLayer.Children.Add(image);

                System.Diagnostics.Debug.WriteLine(
                    $"[Frame {_frameCount}] NEW SPRITE: {item.ImagePath}, pos=({dto.RenderX:F2},{dto.RenderY:F2}), scale={dto.Scale:F2}");
            }

            LayoutSprite(image, dto);
        }

        foreach (var pair in _sprites.Where(x => !visibleIds.Contains(x.Key)).ToList())
        {
            WorldLayer.Children.Remove(pair.Value);
            _sprites.Remove(pair.Key);
        }

        ApplyCamera();
    }

    private void LayoutSprite(Image image, RenderEntityDto dto)
    {
        var normalizedSize = dto.Scale;
        var rect = _viewportService.NormalizedToScreenRect(
            dto.RenderX, dto.RenderY,
            normalizedSize, normalizedSize);

        AbsoluteLayout.SetLayoutFlags(image, AbsoluteLayoutFlags.None);
        AbsoluteLayout.SetLayoutBounds(image, rect);
        image.ZIndex = (int)(dto.Depth * 1000f);
        image.Opacity = dto.ShouldBeOccluded ? 0.0 : 1.0;
    }

    private void ApplyCamera()
    {
        WorldRoot.TranslationX = -_viewportService.CameraX;
    }

    #endregion

    #region Fling Throw Mechanic – PointerGestureRecognizer (platform-safe)

    // ── Pointer events on InputOverlay ────────────────────────────────────────

    private void OnOverlayPointerPressed(object? sender, PointerEventArgs e)
    {
        if (_throwAnimationActive || _overlayVisible)
            return;

        var pos = e.GetPosition(InputOverlay) ?? Point.Zero;
        if (IsInsidePanSlider(pos))
        {
            _throwPointerDown = false;
            _throwModeActive = false;
            _isCameraPanning = false;
            return;
        }

        _throwPressStart   = pos;
        _throwPressCurrent = pos;
        _throwPointerDown  = true;
        _throwModeActive   = IsInsideBallZone(pos);
        _flickHead  = 0;
        _flickCount = 0;

        if (_throwModeActive)
        {
            _isCameraPanning = false;
            System.Diagnostics.Debug.WriteLine(
                $">>> THROW DOWN at ({pos.X:F0},{pos.Y:F0})");
        }
        else
        {
            _camPanLastX     = pos.X;
            _isCameraPanning = true;
        }
    }

    private void OnOverlayPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_overlayVisible)
            return;

        if (!_throwPointerDown) return;

        var pos = e.GetPosition(InputOverlay) ?? _throwPressCurrent;
        _throwPressCurrent = pos;

        if (_throwModeActive)
        {
            float dx = (float)(pos.X - _throwPressStart.X);
            float dy = (float)(pos.Y - _throwPressStart.Y);

            // Record (dx,dy) relative to press-start for flick-speed estimation.
            _flickBuffer[_flickHead % _flickBuffer.Length] =
                new FlickSample(dx, dy, Environment.TickCount64);
            _flickHead++;
            if (_flickCount < _flickBuffer.Length) _flickCount++;

            UpdateTrajectoryPreview(dx, dy);
        }
        else if (_isCameraPanning)
        {
            double delta = pos.X - _camPanLastX;
            _camPanLastX = pos.X;
            _viewportService.PanByScreenDelta(delta);
            ApplyCamera();
        }
    }

    private void OnOverlayPointerReleased(object? sender, PointerEventArgs e)
    {
        if (_overlayVisible)
            return;

        if (!_throwPointerDown) return;
        _throwPointerDown = false;

        var pos = e.GetPosition(InputOverlay) ?? _throwPressCurrent;
        _throwPressCurrent = pos;

        if (_throwModeActive)
        {
            _throwModeActive = false;
            HideTrajectoryPreview();

            float dx = (float)(pos.X - _throwPressStart.X);
            float dy = (float)(pos.Y - _throwPressStart.Y);
            _ = CommitThrowAsync(dx, dy, ComputeFlickSpeedPx());
        }

        _isCameraPanning = false;
    }

    // Mobile touch path for throw control.
    // On Android/iOS this complements desktop pointer handling.
    private void OnThrowBallPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        if (!IsTouchPlatform())
            return;

        if (_overlayVisible || _throwAnimationActive)
            return;

        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _throwPointerDown = true;
                _throwModeActive = true;
                _isCameraPanning = false;
                _flickHead = 0;
                _flickCount = 0;
                _touchThrowDx = 0f;
                _touchThrowDy = 0f;
                break;

            case GestureStatus.Running:
            {
                var dx = (float)e.TotalX;
                var dy = (float)e.TotalY;

                _touchThrowDx = dx;
                _touchThrowDy = dy;

                // Let the control center drift with the finger so touch feels less precise-demanding.
                ThrowOriginBall.TranslationX = Math.Clamp(dx * ThrowControlFollowFactor, -34f, 34f);
                ThrowOriginBall.TranslationY = Math.Clamp(dy * ThrowControlFollowFactor, -26f, 26f);

                _flickBuffer[_flickHead % _flickBuffer.Length] =
                    new FlickSample(dx, dy, Environment.TickCount64);
                _flickHead++;
                if (_flickCount < _flickBuffer.Length) _flickCount++;

                UpdateTrajectoryPreview(dx, dy);
                break;
            }

            case GestureStatus.Canceled:
            case GestureStatus.Completed:
            {
                if (!_throwModeActive)
                    return;

                var dx = Math.Abs(_touchThrowDx) > 0.01f ? _touchThrowDx : (float)e.TotalX;
                var dy = Math.Abs(_touchThrowDy) > 0.01f ? _touchThrowDy : (float)e.TotalY;

                _throwPointerDown = false;
                _throwModeActive = false;
                HideTrajectoryPreview();
                _ = CommitThrowAsync(dx, dy, ComputeFlickSpeedPx());
                _touchThrowDx = 0f;
                _touchThrowDy = 0f;
                _ = ThrowOriginBall.TranslateTo(0, 0, 110, Easing.CubicOut);
                break;
            }
        }
    }

    // ── Hit-zone detection ────────────────────────────────────────────────────

    /// <summary>
    /// Returns true when <paramref name="pos"/> (overlay-relative) falls within
    /// a generous circular hit-zone centred on the resting ThrowOriginBall.
    /// </summary>
    private bool IsInsideBallZone(Point pos)
    {
        double ow = InputOverlay.Width  > 0 ? InputOverlay.Width  : _viewportService.ViewportWidth;
        double oh = InputOverlay.Height > 0 ? InputOverlay.Height : _viewportService.ViewportHeight;

        double ballSize = ThrowOriginBall.Width > 0
            ? ThrowOriginBall.Width
            : (ThrowOriginBall.WidthRequest > 0 ? ThrowOriginBall.WidthRequest : 56.0);

        // Ball resting centre: HorizontalOptions=Center, VerticalOptions=End, Margin bottom 20.
        // Center-Y = overlay height - margin bottom - half ball size.
        double cx = ow / 2.0;
        double cy = oh - 20.0 - (ballSize / 2.0);

        const double HitRadius = 70;  // generous target for mouse and touch
        double ddx = pos.X - cx;
        double ddy = pos.Y - cy;
        return ddx * ddx + ddy * ddy <= HitRadius * HitRadius;
    }

    private bool IsInsidePanSlider(Point pos)
    {
        if (PanSliderRoot.Width <= 0 || PanSliderRoot.Height <= 0)
            return false;

        return pos.X >= PanSliderRoot.X &&
               pos.X <= PanSliderRoot.X + PanSliderRoot.Width &&
               pos.Y >= PanSliderRoot.Y &&
               pos.Y <= PanSliderRoot.Y + PanSliderRoot.Height;
    }

    private void OnPanSliderPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                if (_throwAnimationActive || _overlayVisible)
                    return;

                _panSliderInputNormalized = 0;
                _throwPointerDown = false;
                _throwModeActive = false;
                _isCameraPanning = false;
                break;

            case GestureStatus.Running:
                if (_throwAnimationActive || _overlayVisible)
                    return;

                var clampedThumbX = Math.Clamp(e.TotalX, -PanSliderTravel, PanSliderTravel);
                PanSliderThumb.TranslationX = clampedThumbX;

                var normalized = clampedThumbX / PanSliderTravel;
                if (Math.Abs(normalized) < PanSliderDeadZone)
                {
                    normalized = 0;
                }

                _panSliderInputNormalized = normalized;
                break;

            case GestureStatus.Canceled:
            case GestureStatus.Completed:
                _panSliderInputNormalized = 0;
                _ = PanSliderThumb.TranslateTo(0, 0, 120, Easing.CubicOut);
                break;
        }
    }

    private void ApplyContinuousPanFromSlider(float dt)
    {
        if (_overlayVisible || _throwAnimationActive)
            return;

        if (Math.Abs(_panSliderInputNormalized) < 0.001)
            return;

        // Scale pan speed with map width so larger scenes still feel responsive.
        var mapToViewportRatio = _viewportService.WorldWidth / Math.Max(1.0, _viewportService.ViewportWidth);
        var speedScreensPerSecond = 0.55 + ((mapToViewportRatio - 1.0) * 0.40);
        var speedPixelsPerSecond = _viewportService.ViewportWidth * Math.Clamp(speedScreensPerSecond, 0.45, 1.60);
        var panDelta = _panSliderInputNormalized * speedPixelsPerSecond * dt;

        // Negative here keeps control intuitive: thumb right => camera/view right.
        _viewportService.PanByScreenDelta(-panDelta);
        ApplyCamera();
    }

    // ── Trajectory arc preview ────────────────────────────────────────────────

    private void UpdateTrajectoryPreview(float dx, float dy)
    {
        float dist = MathF.Sqrt(dx * dx + dy * dy);
        if (dist < 12f)
        {
            TrajectoryView.IsVisible = false;
            return;
        }

        float power = (float)Math.Clamp(dist / 120.0, 0.3, 2.5);

        float vw = (float)(TrajectoryView.Width  > 0 ? TrajectoryView.Width  : _viewportService.ViewportWidth);
        float vh = (float)(TrajectoryView.Height > 0 ? TrajectoryView.Height : _viewportService.ViewportHeight);
        float ballHalf = (float)(ThrowOriginBall.Width > 0
            ? ThrowOriginBall.Width * 0.5
            : (ThrowOriginBall.WidthRequest > 0 ? ThrowOriginBall.WidthRequest * 0.5 : 28.0));

        // Ball is FIXED — arc origin is always the resting ball centre.
        float ballCx = vw / 2f;
        float ballCy = vh - 20f - ballHalf;

        _trajectoryDrawable.BallCenter   = new PointF(ballCx, ballCy);
        _trajectoryDrawable.SwipeDeltaX  = dx;
        _trajectoryDrawable.SwipeDeltaY  = dy;
        _trajectoryDrawable.Power        = power;

        TrajectoryView.IsVisible = true;
        TrajectoryView.Invalidate();
    }

    private void HideTrajectoryPreview()
    {
        TrajectoryView.IsVisible = false;
        _trajectoryDrawable.Power = 0f;
    }

    // ── Flick-speed estimation ────────────────────────────────────────────────

    private double ComputeFlickSpeedPx()
    {
        if (_flickCount < 2) return 300.0;

        int newestIdx = (_flickHead - 1 + _flickBuffer.Length) % _flickBuffer.Length;
        var newest = _flickBuffer[newestIdx];

        var oldest = newest;
        for (int i = 1; i < _flickCount; i++)
        {
            int idx = (_flickHead - 1 - i + _flickBuffer.Length * 2) % _flickBuffer.Length;
            oldest = _flickBuffer[idx];
            if (newest.TickMs - oldest.TickMs >= 80) break;
        }

        long dtMs = newest.TickMs - oldest.TickMs;
        if (dtMs < 8) return 300.0;

        float dxS = newest.Dx - oldest.Dx;
        float dyS = newest.Dy - oldest.Dy;
        return MathF.Sqrt(dxS * dxS + dyS * dyS) / (dtMs / 1000.0);
    }

    private async Task CommitThrowAsync(double screenDx, double screenDy, double flickSpeedPx)
    {
        if (_throwAnimationActive || _overlayVisible)
            return;

        var session = _sessions.Current;
        if (session == null || !session.IsRunning)
            return;

        var launch = BuildThrowLaunchInput(session, screenDx, screenDy, flickSpeedPx);
        var projectile = _core.CreateRandomProjectile(_random);
        var simulation = session.World.SimulateDeferredThrow(projectile, launch);

        _throwAnimationActive = true;
        ThrowOriginBall.IsVisible = false;
        ThrowFlyingBall.Scale = 1.0;

        // Play throw sound immediately on release for feedback
        _audio.PlaySfx("throw");

        System.Diagnostics.Debug.WriteLine(
            $"\n=== THROW LAUNCH dir=({launch.DirectionX:F2},{launch.DirectionY:F2})"
            + $" power={launch.Power:F2} loft={launch.Loft:F2}"
            + $" start=({launch.StartX:F2},{launch.StartY:F2})"
            + $" impact=({simulation.ImpactX:F2},{simulation.ImpactY:F2},{simulation.ImpactZ:F2})"
            + $" impactType={simulation.Impact.ImpactType}");

        try
        {
            await AnimateThrowFlightAsync(simulation);

            var impact = session.World.ResolveDeferredThrowSimulation(projectile, simulation);

            session.Throws++;

            if (impact.ImpactType == ProjectileImpactType.Coworker)
            {
                session.Hits++;
                if (impact.Coworker is not null && !impact.Coworker.IsAlive)
                {
                    session.Eliminations++;
                }
            }
            else if (impact.ImpactType == ProjectileImpactType.Wall)
            {
                session.WallHits++;
            }

            // Dispatch impact sounds immediately since EventDispatcher already ran this frame
            DispatchThrowImpactSound(impact);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"CommitThrowAsync ERROR: {ex.Message}");
        }
        finally
        {
            _throwAnimationActive = false;
            ThrowFlyingBall.IsVisible = false;
            ThrowFlyingBall.TranslationX = 0;
            ThrowFlyingBall.TranslationY = 0;
            ThrowOriginBall.IsVisible = true;
        }
    }

    private ThrowLaunchInput BuildThrowLaunchInput(GameSession session, double screenDx, double screenDy, double flickSpeedPx)
    {
        float dx = (float)screenDx;
        float dy = (float)screenDy;
        float dist = MathF.Sqrt(dx * dx + dy * dy);

        if (dist < 12f)
        {
            dx = 0f;
            dy = -150f;
            dist = 150f;
            flickSpeedPx = 350;
        }

        float angleRad = MathF.Atan2(dy, dx);
        float dirX = MathF.Cos(angleRad);
        float dirY = MathF.Sin(angleRad);

        // Power curve: easier short throws, still allows strong full drags.
        var distNorm = (float)Math.Clamp(dist / 220.0, 0.0, 1.0);
        var speedNorm = (float)Math.Clamp(flickSpeedPx / 1200.0, 0.0, 1.0);
        var mixedPower = (distNorm * 0.70f) + (speedNorm * 0.30f);
        var shapedPower = MathF.Pow(mixedPower, 1.55f);
        float power = (float)Math.Clamp(0.18f + (shapedPower * 2.42f), 0.18, 2.60);

        // Loft controls how high the throw travels above the ground plane.
        // All throws now get a minimum loft to ensure good arcs; upward drags and flicks increase further.
        // Even horizontal throws (upwardIntent=0) should arc nicely over obstacles.
        var upwardIntent = Math.Clamp((-dy) / Math.Max(1.0f, dist), 0.0, 1.0);
        var flickBoost = Math.Clamp(flickSpeedPx / 900.0, 0.0, 1.0);
        float loft = (float)Math.Clamp(0.50 + (upwardIntent * 0.50) + (flickBoost * 0.20), 0.40, 1.20);

        float startX = _viewportService.GetCenterNormalizedX();
        // Virtual launch strip: start further in front of the visible scene to exaggerate arc entry.
        // This keeps depth scaling valid while making even short/close throws look more dramatic.
        float frontY = session.World.Map.FrontDepthY;
        float startY = Math.Min(1.12f, frontY + 0.26f);

        return new ThrowLaunchInput(
            StartX: startX,
            StartY: startY,
            DirectionX: dirX,
            DirectionY: dirY,
            Power: power,
            Loft: loft);
    }

    private Task AnimateThrowFlightAsync(ThrowSimulationResult simulation)
    {
        var tcs = new TaskCompletionSource();
        var samples = simulation.Samples;

        System.Diagnostics.Debug.WriteLine(
            $">>> ANIMATION START: flightTime={simulation.FlightTime:F3}s, samples={samples.Count}," +
            $" impact=({simulation.ImpactX:F4},{simulation.ImpactY:F4},{simulation.ImpactZ:F4})");

        if (samples.Count == 0)
        {
            return Task.CompletedTask;
        }

        var start = GetThrowBallCenterInOverlay();
        var cameraXAtThrow = _viewportService.CameraX;
        var startSample = samples[0];
        var sampleStartX = (startSample.X * _viewportService.WorldWidth) - cameraXAtThrow;
        var sampleStartY = (startSample.Y - startSample.Z) * _viewportService.WorldHeight;
        var startOffsetX = start.X - sampleStartX;
        var startOffsetY = start.Y - sampleStartY;

        System.Diagnostics.Debug.WriteLine(
            $"   start=({start.X:F1},{start.Y:F1})" +
            $" sampleStart=({startSample.X:F4},{startSample.Y:F4},{startSample.Z:F4})" +
            $" offset=({startOffsetX:F1},{startOffsetY:F1})");

        // Cinematic stretch: short impacts still get enough screen time for readable arc motion.
        var displayedFlight = Math.Max(simulation.FlightTime * 1.35f, 0.34f);
        var durationMs = (uint)Math.Clamp(displayedFlight * 1000.0, 340.0, 1450.0);
        var startScale = Math.Max(startSample.Scale, 0.001f);

        ThrowFlyingBall.IsVisible = true;
        ThrowFlyingBall.TranslationX = 0;
        ThrowFlyingBall.TranslationY = 0;
        ThrowFlyingBall.Scale = 1.0;

        this.Animate(
            name: "throw_flight",
            callback: t =>
            {
                var sample = SampleAtTime(samples, (float)(t * simulation.FlightTime));
                var x = (sample.X * _viewportService.WorldWidth) - cameraXAtThrow + startOffsetX;
                var y = (sample.Y - sample.Z) * _viewportService.WorldHeight + startOffsetY;

                ThrowFlyingBall.TranslationX = x - start.X;
                ThrowFlyingBall.TranslationY = y - start.Y;
                ThrowFlyingBall.Scale = Math.Clamp(sample.Scale / startScale, 0.45f, 2.50f);
            },
            start: 0.0,
            end: 1.0,
            length: durationMs,
            easing: Easing.Linear,
            finished: (_, __) => 
            {
                System.Diagnostics.Debug.WriteLine(">>> ANIMATION FINISHED");
                tcs.TrySetResult();
            });

        return tcs.Task;
    }

    private static ThrowTrajectorySample SampleAtTime(IReadOnlyList<ThrowTrajectorySample> samples, float time)
    {
        if (samples.Count == 1)
            return samples[0];

        if (time <= samples[0].Time)
            return samples[0];

        var last = samples[^1];
        if (time >= last.Time)
            return last;

        for (int i = 1; i < samples.Count; i++)
        {
            var right = samples[i];
            if (time > right.Time)
            {
                continue;
            }

            var left = samples[i - 1];
            var span = right.Time - left.Time;
            if (span <= 0.0001f)
            {
                return right;
            }

            var t = (time - left.Time) / span;
            return new ThrowTrajectorySample(
                Time: time,
                X: left.X + ((right.X - left.X) * t),
                Y: left.Y + ((right.Y - left.Y) * t),
                Z: left.Z + ((right.Z - left.Z) * t),
                Scale: left.Scale + ((right.Scale - left.Scale) * t));
        }

        return last;
    }

    private Point GetThrowBallCenterInOverlay()
    {
        double overlayW = InputOverlay.Width > 0 ? InputOverlay.Width : _viewportService.ViewportWidth;
        double overlayH = InputOverlay.Height > 0 ? InputOverlay.Height : _viewportService.ViewportHeight;
        double ballSize = ThrowOriginBall.Width > 0
            ? ThrowOriginBall.Width
            : (ThrowOriginBall.WidthRequest > 0 ? ThrowOriginBall.WidthRequest : 56.0);

        return new Point(overlayW / 2.0, overlayH - 20.0 - (ballSize / 2.0));
    }

    private void DispatchThrowImpactSound(ProjectileImpactResult impact)
    {
        var soundKey = impact.ImpactType switch
        {
            ProjectileImpactType.Coworker => "impact_high",
            ProjectileImpactType.CrowdBlocker => "impact_low",
            ProjectileImpactType.Wall => "impact_object",
            ProjectileImpactType.Floor => "missed_shot_floor",
            ProjectileImpactType.DecorObject => "impact_low",
            _ => null
        };

        if (soundKey != null)
        {
            _audio.PlaySfx(soundKey);
        }
    }

    private void OnPauseClicked(object? sender, EventArgs e)
    {
        if (_overlayVisible || _throwAnimationActive)
            return;

        var session = _sessions.Current;
        if (session == null || session.World.IsGameOver)
            return;

        session.IsRunning = false;
        ShowOverlay(OverlayMode.Pause);
    }

    private void OnOverlayResumeClicked(object? sender, EventArgs e)
    {
        var session = _sessions.Current;
        if (session != null)
        {
            if (_overlayMode == OverlayMode.Intro || _overlayMode == OverlayMode.Pause)
            {
                session.IsRunning = true;
            }
        }

        HideOverlay();
    }

    private void OnOverlayRestartClicked(object? sender, EventArgs e)
    {
        RestartGame();
    }

    private async void OnOverlayMainMenuClicked(object? sender, EventArgs e)
    {
        _audio.StopMusic();
        _sessions.Stop();
        await Shell.Current.GoToAsync("//MainMenuPage");
    }

    private void RestartGame()
    {
        _sessions.StartNewGame();

        _sprites.Clear();
        WorldLayer.Children.Clear();

        _throwPointerDown = false;
        _throwModeActive = false;
        _isCameraPanning = false;
        _touchThrowDx = 0f;
        _touchThrowDy = 0f;
        _throwAnimationActive = false;
        _overlayVisible = false;
        _gameOverOverlayShown = false;
        _flickHead = 0;
        _flickCount = 0;

        ThrowFlyingBall.IsVisible = false;
        ThrowFlyingBall.TranslationX = 0;
        ThrowFlyingBall.TranslationY = 0;
        ThrowOriginBall.TranslationX = 0;
        ThrowOriginBall.TranslationY = 0;
        ThrowOriginBall.IsVisible = true;
        HideTrajectoryPreview();
        HideOverlay();

        var session = _sessions.Current;
        if (session != null)
        {
            session.IsRunning = true;
        }

        _audio.PlayMusic("game");
        ShowOverlay(OverlayMode.Intro);
    }

    private void ShowOverlay(OverlayMode mode)
    {
        var session = _sessions.Current;
        if (session == null)
            return;

        _overlayVisible = true;
        _overlayMode = mode;
        GameOverlay.IsVisible = true;

        switch (mode)
        {
            case OverlayMode.Intro:
                OverlayTitleLabel.Text = "Ready To Defend?";
                OverlayResumeButton.IsVisible = true;
                OverlayResumeButton.Text = "Start Game";
                OverlayRestartButton.IsVisible = false;
                OverlayMenuButton.IsVisible = true;
                OverlayStatsLabel.Text =
                    "Drag from the paper ball to aim.\n" +
                    "Release to throw.\n" +
                    "Drag outside the ball to pan the camera.\n\n" +
                    "Headshots are instant kills.\n" +
                    "Body shots take multiple hits.\n" +
                    "If the front line crowds, your shots get blocked. Pan to find new angles.";
                break;

            case OverlayMode.Pause:
                OverlayTitleLabel.Text = "Paused";
                OverlayResumeButton.IsVisible = true;
                OverlayResumeButton.Text = "Resume";
                OverlayRestartButton.IsVisible = true;
                OverlayRestartButton.Text = "Restart";
                OverlayMenuButton.IsVisible = true;
                OverlayStatsLabel.Text = BuildOverlayStats(session);
                break;

            case OverlayMode.GameOver:
                OverlayTitleLabel.Text = "Game Over";
                OverlayResumeButton.IsVisible = false;
                OverlayRestartButton.IsVisible = true;
                OverlayRestartButton.Text = "Play Again";
                OverlayMenuButton.IsVisible = true;
                OverlayStatsLabel.Text = BuildOverlayStats(session);
                break;
        }
    }

    private void HideOverlay()
    {
        _overlayVisible = false;
        _overlayMode = OverlayMode.None;
        GameOverlay.IsVisible = false;
    }

    private static string BuildOverlayStats(GameSession session)
    {
        var world = session.World;
        return
            $"Score: {world.Score}\n" +
            $"Wave: {world.WaveSpawnManager.CurrentWave}\n" +
            $"Throws: {session.Throws}\n" +
            $"Direct Hits: {session.Hits}\n" +
            $"Eliminations: {session.Eliminations}\n" +
            $"Wall Hits: {session.WallHits}\n" +
            $"Frontline Pressure: {world.CoworkersReachedFront}/15";
    }

    private static bool IsTouchPlatform()
    {
        return DeviceInfo.Platform == DevicePlatform.Android ||
               DeviceInfo.Platform == DevicePlatform.iOS;
    }

    #endregion
}