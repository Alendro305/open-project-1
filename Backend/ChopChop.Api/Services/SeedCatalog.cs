using ChopChop.Api.Contracts;

namespace ChopChop.Api.Services;

/// <summary>
/// Server-authoritative catalog mapping a seed item to its yield and grow time. Keeping grow times
/// here (rather than trusting the client) is what makes offline growth tamper-resistant. Extend or
/// load from configuration as the game's item set grows.
/// </summary>
public interface ISeedCatalog
{
    bool TryGet(string seedItemId, out SeedDefinitionDto definition);
    IReadOnlyList<SeedDefinitionDto> All { get; }
}

public sealed class SeedCatalog : ISeedCatalog
{
    private readonly Dictionary<string, SeedDefinitionDto> _byId;

    public SeedCatalog()
    {
        var defs = new[]
        {
            new SeedDefinitionDto("seed_carrot", "carrot", 60),
            new SeedDefinitionDto("seed_tomato", "tomato", 120),
            new SeedDefinitionDto("seed_potato", "potato", 300),
            new SeedDefinitionDto("seed_pumpkin", "pumpkin", 900),
            new SeedDefinitionDto("seed_instant", "instant_fruit", 0), // grows the moment it's watered (testing/debug)
        };
        _byId = defs.ToDictionary(d => d.SeedItemId, StringComparer.OrdinalIgnoreCase);
        All = defs;
    }

    public IReadOnlyList<SeedDefinitionDto> All { get; }

    public bool TryGet(string seedItemId, out SeedDefinitionDto definition)
        => _byId.TryGetValue(seedItemId ?? string.Empty, out definition!);
}
