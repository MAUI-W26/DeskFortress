using DeskFortress.Core.Geometry;

namespace DeskFortress.Core.World;

// Type of world object used by gameplay and impact resolution.
public enum WorldObjectType
{
    Decor,
    FrontWall,
    BackWall
}

// Runtime-ready object in the scene.
// Decor can block movement, receive projectile impacts and apply score penalties.
public sealed class WorldObject
{
    public string Name { get; }
    public WorldObjectType ObjectType { get; }
    public Polygon Polygon { get; }

    public bool BlocksMovement { get; }
    public bool IsHittable { get; }
    public int PropertyPenalty { get; }
    public int Durability { get; private set; }

    public bool IsDestroyed => Durability <= 0;

    public WorldObject(
        string name,
        WorldObjectType objectType,
        Polygon polygon,
        bool blocksMovement,
        bool isHittable,
        int propertyPenalty,
        int durability)
    {
        Name = name;
        ObjectType = objectType;
        Polygon = polygon;
        BlocksMovement = blocksMovement;
        IsHittable = isHittable;
        PropertyPenalty = propertyPenalty;
        Durability = Math.Max(1, durability);
    }

    // Returns true when this hit destroyed the object.
    public bool ApplyHit()
    {
        if (!IsHittable || IsDestroyed)
        {
            return false;
        }

        Durability--;
        return IsDestroyed;
    }
}