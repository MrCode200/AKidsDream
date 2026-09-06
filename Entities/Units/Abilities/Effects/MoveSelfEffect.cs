#nullable enable
using System;
using AKidsDream.Common;
using AKidsDream.Common.Components.TweenComponent.Resources;
using AKidsDream.Common.Errors;
using AKidsDream.Common.Logging;
using AKidsDream.Common.Results;
using Godot;
using Serilog;

namespace AKidsDream.Abilities.Effects;

[GlobalClass]
[Tool]
public partial class MoveSelfEffect : EffectData
{
    private static readonly ILogger Log = GameLogger.For<MoveSelfEffect>();

    public override void UpdatePayload(AbilityContext context, AbilityPayload payload)
    {
        switch (payload.ProcessingTiles.Count)
        {
            // For chaining: update CurrentOrigin to the last target tile.
            // This allows the next target selection to be relative to the new position
            case 0:
                return;
            case > 1:
                Log.Here().Debug("Passed more than one tile to UpdatePayload in MoveSelfEffect, only using last tile");
                break;
        }

        payload.CurrentOrigin = payload.ProcessingTiles[^1];
    }

    public override Result<EffectOutcome, EffectError> ApplyEffect(
        AbilityContext context,
        AbilityPayload payload,
        Vector2I[] affectedTiles)
    {
        if (context.Caster is not Unit castingUnit)
        {
            throw new InvalidOperationException($"Cannot apply MoveSelfEffect to a non-unit caster. " +
                                                $"Caster type: {context.Caster.GetType().Name}");
        }

        if (affectedTiles.Length != 1)
        {
            return Result.Fail<EffectOutcome, EffectError>(
                new EffectError.InvalidTargetCount(Min: 1, Max: 1, Actual: affectedTiles.Length));
        }

        Vector2I from = castingUnit.TileLocation;
        Vector2I to = affectedTiles[0];
        if (!castingUnit.Move(to))
        {
            return Result.Fail<EffectOutcome, EffectError>(
                new EffectError.ExecutionFailed($"Unit move from {from} to tile {to} failed."));
        }

        return Result.Ok<EffectOutcome, EffectError>(new MoveOutcome
        {
            Caster = castingUnit,
            Target = castingUnit,
            From = from,
            To = to
        });
    }
}
