#nullable enable
using System.Collections.Generic;
using AKidsDream.Common;
using Godot;

namespace AKidsDream.Abilities.Effects;

public abstract record EffectOutcome
{
    public IAbilityCaster? Caster { get; init; }
}

public sealed record DamageOutcome : EffectOutcome
{
    public required Unit Target { get; init; }
    public required Vector2I Tile { get; init; }
    public required int Amount { get; init; }
}

public sealed record MoveOutcome : EffectOutcome
{
    public required Unit Target { get; init; }
    public required Vector2I From { get; init; }
    public required Vector2I To { get; init; }
}

public sealed record SummonOutcome : EffectOutcome
{
    public required Unit Summoned { get; init; }
    public required Vector2I Tile { get; init; }
}

public sealed record CompositeOutcome : EffectOutcome
{
    public IReadOnlyList<EffectOutcome> Outcomes { get; init; } = [];

    public static readonly CompositeOutcome Empty = new() { Outcomes = [] };

    public CompositeOutcome() { }

    public CompositeOutcome(IReadOnlyList<EffectOutcome> outcomes, bool flatten = false, int depth = -1)
    {
        Outcomes = flatten ? [.. Flatten(outcomes, depth)] : outcomes;
    }

    public static IEnumerable<EffectOutcome> Flatten(IEnumerable<EffectOutcome> outcomes, int depth = -1)
    {
        foreach (var outcome in outcomes)
        {
            if (outcome is not CompositeOutcome composite || depth == 0)
            {
                yield return outcome;
                continue;
            }

            var nextDepth = depth < -1 ? -1 : depth - 1;
            foreach (var child in Flatten(composite.Outcomes, nextDepth))
            {
                yield return child;
            }
        }
    }
}
