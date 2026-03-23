using DeskFortress.Core.Entities;
using DeskFortress.UI.Assets;

namespace DeskFortress.UI.Rendering;

public sealed class DefaultVisualResolver : IVisualResolver
{
    private readonly AssetRegistry _registry;
    private readonly Random _rng = new();

    public DefaultVisualResolver(AssetRegistry registry)
    {
        _registry = registry;
    }

    public string ResolveVisualPath(object entity)
    {
        return entity switch
        {
            CoworkerEntity => Pick(_registry.CharacterImagePaths),
            ProjectileEntity => Pick(_registry.ProjectileImagePaths),
            _ => throw new InvalidOperationException(
                $"No visual mapping for {entity.GetType().Name}")
        };
    }

    private string Pick(IReadOnlyDictionary<string, string> dict)
    {
        var index = _rng.Next(dict.Count);
        return dict.ElementAt(index).Value;
    }
}