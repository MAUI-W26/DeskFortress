using DeskFortress.Core.Simulation;
using DeskFortress.Core.World;

namespace DeskFortress.UI.Rendering;

public sealed class Renderer
{
    private readonly IVisualResolver _resolver;
    private readonly Dictionary<Guid, string> _visualCache = new();
    private int _frameCount = 0;

    public IReadOnlyList<RenderFrameItem> LastFrame { get; private set; } = Array.Empty<RenderFrameItem>();

    public Renderer(IVisualResolver resolver)
    {
        _resolver = resolver;
    }

    public void Render(GameWorld world)
    {
        _frameCount++;
        
        var items = new List<RenderFrameItem>(
            world.Coworkers.Count + world.Projectiles.Count);

        // Occlusion threshold
        var occlusionThreshold = world.Map.BackDepthY + 
            ((world.Map.FrontDepthY - world.Map.BackDepthY) * 0.4f);

        // Render coworkers
        foreach (var c in world.Coworkers)
        {
            var dto = RenderDtoFactory.ToDto(c, occlusionThreshold);
            items.Add(new RenderFrameItem
            {
                Dto = dto,
                ImagePath = ResolveCached(c, dto.Id)
            });
        }

        // Render projectiles with detailed logging
        foreach (var p in world.Projectiles)
        {
            var dto = RenderDtoFactory.ToDto(p, occlusionThreshold);
            items.Add(new RenderFrameItem
            {
                Dto = dto,
                ImagePath = ResolveCached(p, dto.Id)
            });
            
            // Log every projectile every 30 frames
            if (_frameCount % 30 == 0 || p.State == Core.Entities.ProjectileState.Flying)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[Frame {_frameCount}] Projectile: " +
                    $"Ground=({p.X:F2},{p.Y:F2},{p.Z:F2}), " +
                    $"Render=({dto.RenderX:F2},{dto.RenderY:F2}), " +
                    $"Vel=({p.VX:F2},{p.VY:F2},{p.VZ:F2}), " +
                    $"Scale={dto.Scale:F2}, Depth={dto.Depth:F2}, " +
                    $"State={p.State}, Occluded={dto.ShouldBeOccluded}");
            }
        }

        LastFrame = items.OrderBy(i => i.Dto.Depth).ToArray();
    }

    private string ResolveCached(object entity, Guid id)
    {
        if (_visualCache.TryGetValue(id, out var path))
            return path;

        path = _resolver.ResolveVisualPath(entity);
        _visualCache[id] = path;

        return path;
    }
}