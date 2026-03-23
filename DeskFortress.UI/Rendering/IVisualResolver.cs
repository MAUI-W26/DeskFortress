namespace DeskFortress.UI.Rendering;

/// <summary>
/// Resolves image file paths for game entities.
/// Allows different visual selection strategies (random, deterministic, themed, etc.)
/// without coupling the renderer to specific asset management logic.
/// </summary>
public interface IVisualResolver
{
    /// <summary>
    /// Resolves the image file path for a given entity.
    /// </summary>
    /// <param name="entity">The Core entity (CoworkerEntity, ProjectileEntity, etc.)</param>
    /// <returns>File path to the entity's visual asset</returns>
    string ResolveVisualPath(object entity);
}