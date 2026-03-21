# DeskFortress.Core Diagrams

## 1. High-Level Architecture Flow

```mermaid
flowchart TD
    A[Raw JSON Assets] --> B[AssetLoader]
    B --> C[BackgroundAsset / CharacterAsset / ProjectileAsset]

    C --> D[AssetNormalizer]
    C --> E[AssetMeasureResolver]

    D --> F[Normalized Geometry]
    E --> G[AssetScaleProfile]

    F --> H[BackgroundFactory]
    F --> I[CharacterFactory]
    F --> J[ProjectileFactory]

    G --> H
    G --> I
    G --> J

    H --> K[BackgroundMap]
    I --> L[CoworkerEntity]
    J --> M[ProjectileEntity]

    K --> N[WorldScaleCalculator]
    K --> O[DepthSystem]
    K --> P[SpawnSystem]
    K --> Q[MapCollisionSystem]

    N --> R[BaseScale]
    O --> S[DepthScale]

    R --> T[Entity.Scale]
    S --> T

    T --> U[ShapeTransform]
    U --> V[CollisionSystem]
    T --> W[RenderDtoFactory]
    W --> X[UI Render Payload]
```

## 2. Scale Resolution Flow

```mermaid
flowchart LR
    A[Asset Metadata real_measure] --> B{Asset Type}
    B -->|Background| C[Resolve polygon from group/index]
    B -->|Coworker| D[Return 1.0 normalized height]
    B -->|Projectile| E[Resolve ellipse from collision_shapes]

    C --> F[Measure normalized width/height]
    E --> G[Measure normalized width/height/diameter]

    F --> H[AssetScaleProfile]
    D --> H
    G --> H

    H --> I[GetWorldUnitsPerNormalizedUnit]
    I --> J[WorldScaleCalculator]
    J --> K[Entity.BaseScale]
```

## 3. Runtime Update Flow

```mermaid
flowchart TD
    A[GameWorld.Update dt] --> B[UpdateCoworkers]
    A --> C[UpdateProjectiles]

    B --> D[MapCollisionSystem.ResolveMove]
    D --> E[MovementSystem.RefreshDepthAndScale]

    C --> F[Entity.Integrate]
    F --> G[MovementSystem.RefreshDepthAndScale]
    G --> H[CollisionSystem.CheckProjectileHit]

    H --> I{Hit?}
    I -->|Yes| J[Mark projectile as hit]
    I -->|No| K[Keep projectile alive]

    A --> L[Cleanup dead coworkers / used projectiles]
```

## 4. Package / Module Overview

```mermaid
flowchart TB
    subgraph Assets
        A1[AssetLoader]
        A2[AssetModels]
        A3[BackgroundAsset]
        A4[CharacterAsset]
        A5[ProjectileAsset]
        A6[AssetNormalizer]
        A7[AssetMeasureResolver]
    end

    subgraph Geometry
        G1[Vec2]
        G2[Bounds]
        G3[Polygon]
        G4[EllipseShape]
        G5[ShapeMetrics]
        G6[CollisionHelper]
    end

    subgraph World
        W1[AssetScaleProfile]
        W2[BackgroundMap]
        W3[BackgroundFactory]
        W4[DepthSystem]
        W5[CameraSystem]
        W6[CharacterFactory]
        W7[ProjectileFactory]
        W8[WorldScaleCalculator]
        W9[RenderDtoFactory]
    end

    subgraph Entities
        E1[Entity]
        E2[CoworkerEntity]
        E3[ProjectileEntity]
        E4[BodyPartShape]
        E5[HitZoneType]
    end

    subgraph Simulation
        S1[ShapeTransform]
        S2[SpawnSystem]
        S3[MovementSystem]
        S4[MapCollisionSystem]
        S5[CollisionSystem]
        S6[GameWorld]
    end

    Assets --> Geometry
    Assets --> World
    World --> Entities
    Geometry --> Simulation
    Entities --> Simulation
    World --> Simulation
```

## 5. Core Class Diagram

```mermaid
classDiagram
    class Vec2 {
        +float X
        +float Y
        +Length() float
        +Normalized() Vec2
        +Dot(a, b) float
    }

    class Bounds {
        +float MinX
        +float MinY
        +float MaxX
        +float MaxY
        +Width float
        +Height float
        +Center Vec2
    }

    class Polygon {
        +IReadOnlyList~Vec2~ Points
        +GetBounds() Bounds
    }

    class EllipseShape {
        +Vec2 Center
        +float RadiusX
        +float RadiusY
        +GetBounds() Bounds
    }

    class ShapeMetrics {
        <<static>>
        +GetHeight(polygon) float
        +GetWidth(polygon) float
        +GetHeight(ellipse) float
        +GetWidth(ellipse) float
    }

    class CollisionHelper {
        <<static>>
        +PointInPolygon(point, polygon) bool
        +DistancePointToSegment(point, a, b) float
        +CircleIntersectsPolygon(center, radius, polygon) bool
        +CircleIntersectsEllipse(center, radius, ellipse) bool
    }

    Polygon --> Vec2
    Bounds --> Vec2
    EllipseShape --> Vec2
    Polygon --> Bounds
    EllipseShape --> Bounds
    ShapeMetrics --> Polygon
    ShapeMetrics --> EllipseShape
    CollisionHelper --> Vec2
    CollisionHelper --> Polygon
    CollisionHelper --> EllipseShape
```

## 6. Asset and World Class Diagram

```mermaid
classDiagram
    class AssetSize {
        +float Width
        +float Height
    }

    class AssetMetadata {
        +AssetUnits Units
        +AssetRealMeasure RealMeasure
    }

    class AssetUnits {
        +string Space
        +string RealWorld
    }

    class AssetRealMeasure {
        +string Type
        +AssetMeasureSource Source
        +float Value
    }

    class AssetMeasureSource {
        +string Kind
        +string Group
        +int Index
        +string Axis
    }

    class BackgroundAsset {
        +string Type
        +AssetMetadata Metadata
        +AssetSize OriginalSize
        +List~JsonPolygon~ SpawnZones
        +List~JsonPolygon~ Floor
        +List~JsonPolygon~ FrontWalls
        +List~JsonPolygon~ BackWalls
        +List~JsonPolygon~ DecorObjects
    }

    class CharacterAsset {
        +string Type
        +AssetMetadata Metadata
        +AssetSize OriginalSize
        +List~JsonEllipse~ Head
        +List~JsonPolygon~ Chest
        +List~JsonPolygon~ LeftArm
        +List~JsonPolygon~ RightArm
        +List~JsonPolygon~ LeftLeg
        +List~JsonPolygon~ RightLeg
        +List~JsonPolygon~ ExtraObjects
    }

    class ProjectileAsset {
        +string Type
        +AssetMetadata Metadata
        +AssetSize OriginalSize
        +List~JsonEllipse~ CollisionShapes
    }

    class AssetNormalizer {
        <<static>>
        +NormalizePoint(point, size) Vec2
        +NormalizePolygon(polygon, size) Polygon
        +NormalizeEllipse(ellipse, size) EllipseShape
    }

    class AssetMeasureResolver {
        <<static>>
        +ResolveNormalizedMeasure(background) float
        +ResolveNormalizedMeasure(character) float
        +ResolveNormalizedMeasure(projectile) float
    }

    class AssetScaleProfile {
        +string MeasureType
        +float RealValue
        +float NormalizedMeasure
        +GetWorldUnitsPerNormalizedUnit() float
    }

    class BackgroundMap {
        +IReadOnlyList~Polygon~ SpawnZones
        +IReadOnlyList~Polygon~ Floor
        +IReadOnlyList~Polygon~ FrontWalls
        +IReadOnlyList~Polygon~ BackWalls
        +IReadOnlyList~NamedPolygon~ DecorObjects
        +AssetScaleProfile ScaleProfile
        +float BackDepthY
        +float FrontDepthY
    }

    class BackgroundFactory {
        <<static>>
        +Create(asset) BackgroundMap
    }

    class WorldScaleCalculator {
        +GetEntityWorldScale(entity) float
    }

    AssetMetadata --> AssetUnits
    AssetMetadata --> AssetRealMeasure
    AssetRealMeasure --> AssetMeasureSource

    BackgroundAsset --> AssetMetadata
    BackgroundAsset --> AssetSize
    CharacterAsset --> AssetMetadata
    CharacterAsset --> AssetSize
    ProjectileAsset --> AssetMetadata
    ProjectileAsset --> AssetSize

    AssetNormalizer --> BackgroundAsset
    AssetNormalizer --> CharacterAsset
    AssetNormalizer --> ProjectileAsset

    AssetMeasureResolver --> BackgroundAsset
    AssetMeasureResolver --> CharacterAsset
    AssetMeasureResolver --> ProjectileAsset

    BackgroundFactory --> BackgroundAsset
    BackgroundFactory --> BackgroundMap
    BackgroundMap --> AssetScaleProfile
    WorldScaleCalculator --> AssetScaleProfile
```

## 7. Entity and Simulation Class Diagram

```mermaid
classDiagram
    class Entity {
        <<abstract>>
        +Guid Id
        +float X
        +float Y
        +float VX
        +float VY
        +float Depth
        +float BaseScale
        +float DepthScale
        +float Scale
        +AssetScaleProfile ScaleProfile
        +bool IsAlive
        +Integrate(dt)
    }

    class CoworkerEntity {
        +List~BodyPartShape~ LocalShapes
    }

    class ProjectileEntity {
        +bool HasHit
        +List~EllipseShape~ LocalShapes
    }

    class BodyPartShape {
        +HitZoneType ZoneType
        +Polygon Polygon
        +EllipseShape Ellipse
    }

    class HitZoneType {
        <<enum>>
        Head
        Chest
        LeftArm
        RightArm
        LeftLeg
        RightLeg
        ExtraObject
    }

    class ShapeTransform {
        <<static>>
        +ToWorldPolygon(entity, polygon) Polygon
        +ToWorldEllipse(coworker, ellipse) EllipseShape
        +ToWorldEllipse(projectile, ellipse) EllipseShape
    }

    class SpawnSystem {
        +SpawnCoworker(entity)
    }

    class MovementSystem {
        +Update(entity, dt)
        +RefreshDepthAndScale(entity)
    }

    class MapCollisionSystem {
        +CanOccupy(point) bool
        +ResolveMove(entity, dt)
    }

    class ProjectileHitResult {
        +CoworkerEntity Target
        +HitZoneType ZoneType
    }

    class CollisionSystem {
        +CheckProjectileHit(projectile, coworkers) ProjectileHitResult
    }

    class GameWorld {
        +BackgroundMap Map
        +DepthSystem DepthSystem
        +WorldScaleCalculator WorldScaleCalculator
        +List~CoworkerEntity~ Coworkers
        +List~ProjectileEntity~ Projectiles
        +SpawnSystem SpawnSystem
        +MovementSystem MovementSystem
        +MapCollisionSystem MapCollisionSystem
        +CollisionSystem CollisionSystem
        +SpawnCoworker(entity)
        +AddProjectile(projectile)
        +Update(dt)
    }

    Entity <|-- CoworkerEntity
    Entity <|-- ProjectileEntity
    CoworkerEntity --> BodyPartShape
    BodyPartShape --> HitZoneType
    BodyPartShape --> Polygon
    BodyPartShape --> EllipseShape
    ProjectileEntity --> EllipseShape

    ShapeTransform --> CoworkerEntity
    ShapeTransform --> ProjectileEntity
    SpawnSystem --> CoworkerEntity
    MovementSystem --> Entity
    MapCollisionSystem --> Entity
    CollisionSystem --> ProjectileEntity
    CollisionSystem --> CoworkerEntity
    CollisionSystem --> ProjectileHitResult
    GameWorld --> SpawnSystem
    GameWorld --> MovementSystem
    GameWorld --> MapCollisionSystem
    GameWorld --> CollisionSystem
```

## 8. Asset Loading Sequence

```mermaid
sequenceDiagram
    participant JSON as Asset JSON
    participant Loader as AssetLoader
    participant Asset as Typed Asset DTO
    participant Resolver as AssetMeasureResolver
    participant Factory as BackgroundFactory/CharacterFactory/ProjectileFactory
    participant Runtime as Runtime Object

    JSON->>Loader: LoadFromJson<T>()
    Loader->>Asset: Deserialize JSON
    Loader-->>Asset: Typed asset

    Asset->>Resolver: ResolveNormalizedMeasure(asset)
    Resolver-->>Factory: normalized measure

    Factory->>Factory: Normalize shapes
    Factory->>Factory: Build AssetScaleProfile
    Factory-->>Runtime: BackgroundMap / Entity
```

## 9. Coworker Spawn Sequence

```mermaid
sequenceDiagram
    participant GameWorld
    participant Scale as WorldScaleCalculator
    participant Spawn as SpawnSystem
    participant Depth as DepthSystem
    participant List as Coworkers[]

    GameWorld->>Scale: GetEntityWorldScale(coworker)
    Scale-->>GameWorld: baseScale
    GameWorld->>GameWorld: coworker.BaseScale = baseScale

    GameWorld->>Spawn: SpawnCoworker(coworker)
    Spawn->>Spawn: Pick random spawn zone
    Spawn->>Spawn: Sample valid point inside polygon
    Spawn->>Depth: GetDepth(y)
    Depth-->>Spawn: depth
    Spawn->>Depth: GetCharacterDepthScale(y)
    Depth-->>Spawn: depthScale
    Spawn-->>GameWorld: coworker positioned and initialized

    GameWorld->>List: Add(coworker)
```

## 10. Projectile Hit Sequence

```mermaid
sequenceDiagram
    participant GameWorld
    participant Move as MovementSystem
    participant Collision as CollisionSystem
    participant Transform as ShapeTransform
    participant Coworker as CoworkerEntity

    GameWorld->>Move: RefreshDepthAndScale(projectile)
    Move-->>GameWorld: projectile.Scale updated

    GameWorld->>Collision: CheckProjectileHit(projectile, coworkers)
    Collision->>Transform: ToWorldEllipse(projectile, localEllipse)
    Transform-->>Collision: projectile world shape

    loop each coworker / each body part
        Collision->>Transform: ToWorldPolygon / ToWorldEllipse(coworker, localShape)
        Transform-->>Collision: world hit zone
        Collision->>Collision: Test intersection
    end

    alt hit found
        Collision-->>GameWorld: ProjectileHitResult
        GameWorld->>GameWorld: projectile.HasHit = true
    else no hit
        Collision-->>GameWorld: null
    end
```

## 11. UI Boundary Flow

```mermaid
flowchart LR
    A[GameWorld / Entities] --> B[RenderDtoFactory]
    B --> C[RenderEntityDto]
    C --> D[UI]

    subgraph Included in DTO
        E[X]
        F[Y]
        G[Scale]
        H[Depth]
        I[Id]
    end

    C --> E
    C --> F
    C --> G
    C --> H
    C --> I

    subgraph Kept inside Core
        J[Metadata resolution]
        K[Collision shapes]
        L[World scale math]
        M[Depth system]
    end

    A --> J
    A --> K
    A --> L
    A --> M
```

## 12. Recommended Reading Order

```mermaid
flowchart TD
    A[1 AssetModels] --> B[2 AssetNormalizer]
    B --> C[3 AssetMeasureResolver]
    C --> D[4 AssetScaleProfile]
    D --> E[5 BackgroundFactory / CharacterFactory / ProjectileFactory]
    E --> F[6 Entity]
    F --> G[7 DepthSystem]
    G --> H[8 ShapeTransform]
    H --> I[9 CollisionSystem]
    I --> J[10 GameWorld]
    J --> K[11 RenderDtoFactory]
```

## 13. Notes

- `BackgroundAsset` defines the world reference scale.
- `CharacterAsset` uses the invariant: full asset height = full character height.
- `ProjectileAsset` uses collision geometry as its physical size source.
- `BaseScale` handles real-world proportion.
- `DepthScale` handles perspective.
- `Entity.Scale = BaseScale * DepthScale` is the critical alignment rule for both rendering and collision.
- The UI receives only minimal render payloads and does not need asset metadata or geometry logic.