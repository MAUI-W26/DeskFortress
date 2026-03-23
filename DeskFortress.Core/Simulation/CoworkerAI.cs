using System;
using DeskFortress.Core.Entities;

namespace DeskFortress.Core.Simulation;

/// <summary>
/// Manages coworker movement AI with erratic lateral steering.
/// Coworkers keep pushing toward the front, but wall bounces and crowd bumps
/// reinforce left-right movement so crowds feel less orderly.
/// </summary>
public sealed class CoworkerAI
{
    private readonly Random _random = new();
    private readonly Dictionary<Guid, MovementState> _states = new();

    private const float MinForwardSpeed = 0.05f;
    private const float MaxForwardSpeed = 0.12f;
    private const float MaxStrafeSpeed = 0.14f;
    private const float MinStrafeDuration = 0.35f;
    private const float MaxStrafeDuration = 1.05f;
    private const float LateralImpulseDecay = 2.4f;
    private const float MaxLateralImpulse = 0.18f;
    private const float WallBounceImpulse = 0.11f;
    private const float CrowdBumpImpulse = 0.07f;
    private const float MinimumForwardRatio = 0.55f;

    private sealed class MovementState
    {
        public float TargetStrafeSpeed;
        public float DesiredForwardSpeed;
        public float RetargetTimer;
        public float LateralImpulse;
        public float Agitation;
    }

    /// <summary>
    /// Updates coworker movement using a forward drive plus a changing strafe intent.
    /// External bump impulses decay over time instead of being reset immediately.
    /// </summary>
    public void UpdateMovement(CoworkerEntity coworker, float baseSpeed = 1.0f, float dt = 0.016f)
    {
        if (!coworker.IsAlive)
            return;

        var state = GetOrCreateState(coworker.Id);

        state.RetargetTimer -= dt;
        state.Agitation = MathF.Max(0f, state.Agitation - (dt * 0.65f));
        state.LateralImpulse = MoveToward(state.LateralImpulse, 0f, LateralImpulseDecay * dt);

        if (state.RetargetTimer <= 0f)
        {
            RetargetStrafe(state, preferredDirection: -MathF.Sign(coworker.VX));
        }

        var desiredVX = (state.TargetStrafeSpeed * baseSpeed) + state.LateralImpulse;
        desiredVX = Math.Clamp(desiredVX, -MaxStrafeSpeed * baseSpeed, MaxStrafeSpeed * baseSpeed);

        var lateralLoad = MathF.Abs(desiredVX) / MathF.Max(0.001f, MaxStrafeSpeed * baseSpeed);
        var targetVY = state.DesiredForwardSpeed * baseSpeed * (1f - (lateralLoad * 0.30f));
        var minForwardSpeed = MinForwardSpeed * baseSpeed * MinimumForwardRatio;
        targetVY = MathF.Max(minForwardSpeed, targetVY);

        coworker.VX = Lerp(coworker.VX, desiredVX, MathF.Min(1f, 0.20f + (dt * 5.0f)));
        coworker.VY = Lerp(coworker.VY, targetVY, MathF.Min(1f, 0.14f + (dt * 3.0f)));

        if (coworker.VY < minForwardSpeed)
        {
            coworker.VY = minForwardSpeed;
        }

        var currentSpeed = MathF.Sqrt((coworker.VX * coworker.VX) + (coworker.VY * coworker.VY));
        var maxCombinedSpeed = MathF.Sqrt((MaxStrafeSpeed * MaxStrafeSpeed) + (MaxForwardSpeed * MaxForwardSpeed)) * baseSpeed;
        if (currentSpeed > maxCombinedSpeed)
        {
            var scale = maxCombinedSpeed / currentSpeed;
            coworker.VX *= scale;
            coworker.VY *= scale;
        }
    }

    /// <summary>
    /// Reinforces a wall bounce by immediately pushing steering away from the block.
    /// </summary>
    public void NotifyWallBounce(Guid id, float pushDirection)
    {
        var state = GetOrCreateState(id);
        var direction = ResolveDirection(pushDirection, fallbackDirection: RandomSign());

        state.LateralImpulse = Math.Clamp(
            state.LateralImpulse + (direction * WallBounceImpulse),
            -MaxLateralImpulse,
            MaxLateralImpulse);
        state.Agitation = MathF.Min(1f, state.Agitation + 0.35f);

        RetargetStrafe(state, direction);
        state.RetargetTimer = MathF.Min(state.RetargetTimer, RandomRange(0.18f, 0.40f));
    }

    /// <summary>
    /// Reinforces a lateral shove from nearby crowd collisions.
    /// The AI keeps the impulse around long enough to visibly scatter the crowd.
    /// </summary>
    public void NotifyCoworkerBump(Guid id, float lateralPush)
    {
        var state = GetOrCreateState(id);
        var push = Math.Clamp(lateralPush, -CrowdBumpImpulse, CrowdBumpImpulse);

        if (MathF.Abs(push) < 0.01f)
        {
            push = RandomSign() * CrowdBumpImpulse * 0.5f;
        }

        state.LateralImpulse = Math.Clamp(state.LateralImpulse + push, -MaxLateralImpulse, MaxLateralImpulse);
        state.Agitation = MathF.Min(1f, state.Agitation + 0.18f);

        if (MathF.Abs(push) > 0.02f)
        {
            state.TargetStrafeSpeed = Math.Clamp(
                state.TargetStrafeSpeed + (MathF.Sign(push) * CrowdBumpImpulse),
                -MaxStrafeSpeed,
                MaxStrafeSpeed);
            state.RetargetTimer = MathF.Min(state.RetargetTimer, RandomRange(0.12f, 0.28f));
        }
    }

    /// <summary>
    /// Checks if a coworker has reached the front danger zone.
    /// </summary>
    public bool HasReachedFront(CoworkerEntity coworker, float frontThreshold)
    {
        return coworker.Y >= frontThreshold;
    }

    /// <summary>
    /// Cleans up tracking data for removed coworkers (prevents memory leaks).
    /// </summary>
    public void RemoveCoworkerTracking(Guid id)
    {
        _states.Remove(id);
    }

    private MovementState GetOrCreateState(Guid id)
    {
        if (_states.TryGetValue(id, out var state))
        {
            return state;
        }

        state = new MovementState();
        _states[id] = state;
        RetargetStrafe(state, preferredDirection: RandomSign());
        state.RetargetTimer = 0f;
        return state;
    }

    private void RetargetStrafe(MovementState state, float preferredDirection)
    {
        var direction = ResolveDirection(preferredDirection, fallbackDirection: RandomSign());

        if (_random.NextDouble() < 0.40)
        {
            direction *= -1f;
        }

        var intensity = 0.45f + (state.Agitation * 0.45f);
        state.TargetStrafeSpeed = direction * RandomRange(0.04f, MaxStrafeSpeed * intensity + 0.04f);
        state.DesiredForwardSpeed = RandomRange(MinForwardSpeed, MaxForwardSpeed);
        state.RetargetTimer = RandomRange(MinStrafeDuration, MaxStrafeDuration) * (1f - (state.Agitation * 0.20f));
    }

    private float RandomRange(float min, float max)
        => min + ((float)_random.NextDouble() * (max - min));

    private float ResolveDirection(float direction, float fallbackDirection)
    {
        if (direction > 0f)
            return 1f;

        if (direction < 0f)
            return -1f;

        return fallbackDirection;
    }

    private float RandomSign()
        => _random.Next(0, 2) == 0 ? -1f : 1f;

    private float MoveToward(float value, float target, float maxDelta)
    {
        if (value < target)
            return MathF.Min(value + maxDelta, target);

        return MathF.Max(value - maxDelta, target);
    }

    private float Lerp(float a, float b, float t)
        => a + ((b - a) * t);
}
