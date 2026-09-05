#nullable enable
using System.Collections.Generic;
using AKidsDream.Common.Components.TweenComponent.Resources;
using AKidsDream.Common.Errors;
using AKidsDream.Common.Results;
using Godot;

namespace AKidsDream.Abilities.Effects;

[GlobalClass]
[Tool]
public partial class DamageEffect : EffectData
{
    [Export] public int Amount = 1;

    public override Result<EffectOutcome, EffectError> ApplyEffect(
        AbilityContext context,
        AbilityPayload payload,
        Vector2I[] affectedTiles)
    {
        List<EffectOutcome> outcomes = [];

        foreach (var tile in affectedTiles)
        {
            if (!context.GameContext.Board.TryGetUnitAt(tile, out var target)) continue;

            target.HealthComp.Damage(Amount);
            outcomes.Add(new DamageOutcome
            {
                Caster = context.Caster,
                Target = target,
                Tile = tile,
                Amount = Amount
            });
        }

        if (outcomes.Count == 0)
            return Result.Ok<EffectOutcome, EffectError>(CompositeOutcome.Empty);

        return outcomes.Count == 1
            ? Result.Ok<EffectOutcome, EffectError>(outcomes[0])
            : Result.Ok<EffectOutcome, EffectError>(new CompositeOutcome { Outcomes = outcomes, Caster = context.Caster });
    }
}
