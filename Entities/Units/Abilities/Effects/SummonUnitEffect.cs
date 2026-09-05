#nullable enable
using System.Collections.Generic;
using AKidsDream.Common;
using AKidsDream.Common.Components.TweenComponent.Resources;
using AKidsDream.Common.Errors;
using AKidsDream.Common.Results;
using Godot;

namespace AKidsDream.Abilities.Effects;

[GlobalClass]
[Tool]
public partial class SummonUnitEffect : EffectData
{
    [Export] public required PackedScene SummonedUnit;
    [Export] public required UnitStatsData SummonedUnitStats;

    public override Result<EffectOutcome, EffectError> ApplyEffect(
        AbilityContext context,
        AbilityPayload payload,
        Vector2I[] affectedTiles)
    {
        var outcomes = new List<EffectOutcome>();
        foreach (var tile in affectedTiles)
        {
            if (!context.GameContext.PlayerTeamRegistry.TryGetPlayer(context.Caster.OwnerId, out var playerData))
            {
                return Result.Fail<EffectOutcome, EffectError>(
                    new EffectError.ExecutionFailed($"PlayerData for owner ID {context.Caster.OwnerId} not found."));
            }

            var summoned = SummonedUnit.Instantiate<Unit>();
            summoned.Init(
                playerData,
                tile,
                SummonedUnitStats,
                context.GameContext.Board
            );
            context.GameContext.EntityLayer.AddChild(summoned);

            outcomes.Add(new SummonOutcome
            {
                Caster = context.Caster,
                Summoned = summoned,
                Tile = tile
            });
        }

        return outcomes.Count == 1
            ? Result.Ok<EffectOutcome, EffectError>(outcomes[0])
            : Result.Ok<EffectOutcome, EffectError>(new CompositeOutcome
            {
                Caster = context.Caster,
                Outcomes = outcomes
            });
    }
}