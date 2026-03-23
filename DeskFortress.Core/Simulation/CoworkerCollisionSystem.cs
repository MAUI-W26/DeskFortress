using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DeskFortress.Core.Entities;

namespace DeskFortress.Core.Simulation;

/// <summary>
/// Handles collision detection and response between coworkers.
/// Prevents overlapping and creates natural crowding behavior.
/// </summary>
public sealed class CoworkerCollisionSystem
{
    private const float BaseCollisionRadius = 0.035f; // Base radius in normalized space
    private const float LateralScatterStrength = 0.06f;
    private const float MomentumTransferStrength = 0.35f;

    /// <summary>
    /// Resolves collisions between all coworkers.
    /// Separates overlapping entities and applies bump physics.
    /// </summary>
    public void ResolveCollisions(List<CoworkerEntity> coworkers, CoworkerAI coworkerAI)
    {
        // Check each pair of coworkers
        for (int i = 0; i < coworkers.Count; i++)
        {
            for (int j = i + 1; j < coworkers.Count; j++)
            {
                var a = coworkers[i];
                var b = coworkers[j];

                if (!a.IsAlive || !b.IsAlive)
                    continue;

                ResolveCollision(a, b, coworkerAI);
            }
        }
    }

    private void ResolveCollision(CoworkerEntity a, CoworkerEntity b, CoworkerAI coworkerAI)
    {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        var distanceSquared = (dx * dx) + (dy * dy);

        // Collision radius scales with entity scale (perspective-aware)
        var radiusA = BaseCollisionRadius * a.Scale;
        var radiusB = BaseCollisionRadius * b.Scale;
        var combinedRadius = radiusA + radiusB;
        var combinedRadiusSquared = combinedRadius * combinedRadius;

        if (distanceSquared >= combinedRadiusSquared)
            return; // No collision

        // Handle exact overlap (extremely rare but possible)
        if (distanceSquared < 0.0001f)
        {
            a.X -= 0.01f;
            b.X += 0.01f;
            coworkerAI.NotifyCoworkerBump(a.Id, -LateralScatterStrength);
            coworkerAI.NotifyCoworkerBump(b.Id, LateralScatterStrength);
            return;
        }

        var distance = MathF.Sqrt(distanceSquared);
        var overlap = combinedRadius - distance;

        // Separation vector (normalized)
        var nx = dx / distance;
        var ny = dy / distance;

        // Separate entities proportionally to their sizes
        var totalMass = a.Scale + b.Scale;
        var separationA = overlap * (b.Scale / totalMass);
        var separationB = overlap * (a.Scale / totalMass);

        a.X -= nx * separationA;
        a.Y -= ny * separationA;
        b.X += nx * separationB;
        b.Y += ny * separationB;

        // Apply bump physics with an added lateral shove so the crowd keeps scattering.
        var relativeVX = b.VX - a.VX;
        var relativeVY = b.VY - a.VY;

        var impulseA = MomentumTransferStrength * (b.Scale / totalMass);
        var impulseB = MomentumTransferStrength * (a.Scale / totalMass);
        var lateralScatter = LateralScatterStrength + (overlap * 0.75f);

        a.VX -= nx * impulseA * relativeVX;
        a.VY -= ny * impulseA * relativeVY;
        b.VX += nx * impulseB * relativeVX;
        b.VY += ny * impulseB * relativeVY;

        a.VX -= nx * lateralScatter * (b.Scale / totalMass);
        b.VX += nx * lateralScatter * (a.Scale / totalMass);

        coworkerAI.NotifyCoworkerBump(a.Id, -nx * lateralScatter * 1.5f);
        coworkerAI.NotifyCoworkerBump(b.Id, nx * lateralScatter * 1.5f);

        // Ensure forward movement isn't completely stopped
        // This prevents coworkers from getting stuck pushing against each other
        const float minForwardSpeed = 0.02f;
        if (a.VY < minForwardSpeed) a.VY = minForwardSpeed;
        if (b.VY < minForwardSpeed) b.VY = minForwardSpeed;
    }
}
