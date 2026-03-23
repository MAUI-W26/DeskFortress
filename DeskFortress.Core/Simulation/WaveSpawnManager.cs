using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeskFortress.Core.Simulation;

/// <summary>
/// Manages continuous wave-based spawning with accelerating difficulty.
/// Creates an increasingly challenging stream of coworkers over time.
/// </summary>
public sealed class WaveSpawnManager
{
    private float _timeSinceLastSpawn;
    private float _gameTime;

    // Difficulty curve configuration
    private const float InitialSpawnInterval = 2.5f;    // Start: spawn every 2.5 seconds
    private const float MinSpawnInterval = 0.4f;        // End: spawn every 0.4 seconds
    private const float AccelerationTime = 180f;        // Reach max difficulty after 3 minutes
    private const int MaxActiveCoworkers = 25;          // Cap to prevent performance issues

    public int TotalSpawned { get; private set; }
    public int CurrentWave { get; private set; }

    /// <summary>
    /// Updates spawn timing and returns true if a new coworker should spawn.
    /// Spawn rate accelerates over time based on difficulty curve.
    /// </summary>
    public bool ShouldSpawn(float dt, int currentCoworkerCount)
    {
        _gameTime += dt;
        _timeSinceLastSpawn += dt;

        // Don't spawn if at max capacity
        if (currentCoworkerCount >= MaxActiveCoworkers)
            return false;

        // Calculate difficulty progression (0.0 to 1.0)
        var difficulty = MathF.Min(_gameTime / AccelerationTime, 1.0f);
        
        // Exponential curve for more dramatic acceleration
        difficulty = difficulty * difficulty;

        // Calculate current spawn interval
        var currentInterval = InitialSpawnInterval - (difficulty * (InitialSpawnInterval - MinSpawnInterval));

        // Update wave number (new wave every 30 seconds)
        CurrentWave = 1 + (int)(_gameTime / 30f);

        // Check if it's time to spawn
        if (_timeSinceLastSpawn >= currentInterval)
        {
            _timeSinceLastSpawn = 0f;
            TotalSpawned++;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets the movement speed multiplier for current difficulty level.
    /// </summary>
    public float GetCurrentSpeedMultiplier()
    {
        var difficulty = MathF.Min(_gameTime / AccelerationTime, 1.0f);
        return 0.88f + (difficulty * 0.30f); // Speed increases from 0.88x to 1.18x
    }

    /// <summary>
    /// Gets the current difficulty as a 0-1 value for UI display.
    /// </summary>
    public float GetDifficultyRatio()
    {
        return MathF.Min(_gameTime / AccelerationTime, 1.0f);
    }
}
