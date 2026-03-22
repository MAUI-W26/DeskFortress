# DeskFortress.Core

Core simulation library for a 2.5D projectile-based game. No UI, no framework dependencies. Designed to be consumed by a MAUI frontend.

---

## Overview

Provides:

* Asset-driven world (JSON → runtime)
* Real-world scaling (feet-based normalization)
* Anchor-based rendering
* 2.5D projectile physics (Z axis)
* Polygon + ellipse collision
* Unified impact model
* Minimal render DTOs

---

## Architecture

```
Assets (JSON)
   ↓
AssetLoader / Normalizer / MeasureResolver
   ↓
Factories (Background / Character / Projectile)
   ↓
World (BackgroundMap + ScaleProfile)
   ↓
GameWorld (Simulation systems)
   ↓
RenderDtoFactory → UI
```

---

## Core Concepts

### Normalized Space

All asset geometry is converted from pixel space → normalized [0..1].

---

### Real-World Scaling

Each asset defines a real-world measure:

```
world_scale = entity_units / background_units
```

---

### Anchor-Based Positioning

* X/Y = ground contact point
* AnchorLocal defines pivot inside sprite

Default fallback:

```
(0.5, 1.0)
```

---

### Depth / Perspective

Y drives scale interpolation via DepthSystem.

---

### 2.5D Physics

```
X/Y → ground
Z   → height

RenderY = Y - Z
```

---

## Public API Usage

### 1. Load Assets

```
var bg = AssetLoader.LoadFromJson<BackgroundAsset>(bgJson);
var cw = AssetLoader.LoadFromJson<CharacterAsset>(cwJson);
var proj = AssetLoader.LoadFromJson<ProjectileAsset>(projJson);
```

---

### 2. Build Runtime Objects

```
var map = BackgroundFactory.Create(bg);

var coworker = CharacterFactory.Create(cw);
var projectile = ProjectileFactory.Create(proj);
```

---

### 3. Create World

```
var world = new GameWorld(map);
```

Exposed:

* world.Coworkers
* world.Projectiles
* world.Events
* world.Score

---

### 4. Spawn Coworkers

```
world.SpawnCoworker(coworker);
```

Effect:

* Applies real-world scale
* Places inside spawn zone
* Initializes depth + velocity

---

### 5. Add / Throw Projectile

Direct:

```
world.AddProjectile(projectile);
```

Recommended:

```
world.ThrowProjectile(
    projectile,
    x: 0.5f,
    y: 0.9f,
    z: 0.1f,
    vx: 0f,
    vy: -0.5f,
    vz: 1.2f
);
```

---

### 6. Update Loop

```
world.Update(dt);
```

This runs:

* Movement
* Depth scaling
* Collision
* Impact resolution
* State transitions

---

### 7. Read Events

```
foreach (var e in world.Events)
{
    // e.Type
    // e.ScoreDelta
}
```

Event types:

* projectile_hit_coworker
* projectile_hit_property
* projectile_hit_wall
* projectile_hit_floor
* projectile_out_of_bounds

---

### 8. Render Data

```
var dto = RenderDtoFactory.ToDto(entity);
```

Fields:

* GroundX / GroundY
* RenderX / RenderY
* Scale
* Depth
* Altitude

Use:

* RenderX/Y for drawing
* GroundX/Y for UI overlays

---

## Entity Lifecycle

### Coworker

* Spawned → moves
* Hit → removed (IsAlive = false)

---

### Projectile

States:

* Flying
* Landed
* Embedded
* Expired

Transitions handled automatically by GameWorld.

---

## Coordinate Model

```
(0,0) --------> X
  |
  |
  v
  Y
```

* Y increases toward player
* Depth derived from Y

---

## JSON Requirements (Minimal)

### Character

```
metadata.anchor → optional but recommended
real_measure.value → required
```

### Background

```
must define:
- floor
- spawn_zones
- front_walls
- back_walls
```

### Projectile

```
must define:
- collision_shapes
```

---

## Responsibilities

Core handles:

* Physics
* Collision
* Scaling
* Simulation

UI must handle:

* Rendering
* Input
* Audio (SFX / music)

---

## Integration Notes (MAUI)

* Call `Update(dt)` from game loop
* Convert DTO → UI transforms
* Play SFX based on world.Events
* Do not reimplement collision or scaling in UI

---

## Summary

DeskFortress.Core is a deterministic simulation layer:

* Input → GameWorld
* Output → DTO + Events

Everything else (rendering, audio, UI) is external.
