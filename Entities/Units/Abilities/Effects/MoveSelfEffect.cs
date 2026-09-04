#nullable enable
using AKidsDream.Common;
using AKidsDream.Common.Logging;
using AKidsDream.Common.Components.TweenComponent.Resources;
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

    public override EffectResult ApplyEffect(AbilityContext context, AbilityPayload payload, Vector2I[] affectedTiles)
    {
        if (context.Caster is not Unit castingUnit)
            return new ErrorResult
            {
                Caster = context.Caster,
                Effect = this,
                Error = "Cannot request MoveSelfEffect to be applied to a non-unit"
            };
        
        if (affectedTiles.Length == 0)
        {
            return new ErrorResult
            {
                Caster = castingUnit,
                Effect = this,
                Error = "MoveSelfEffect pattern returned no tiles"
            };
        }

        Vector2I from = castingUnit.TileLocation;
        Vector2I to = affectedTiles[0];
        if (!castingUnit.Move(to))
            return new ErrorResult()
            {
                Caster = castingUnit,
                Effect = this,
                Error = "Unit Move function returned false"
            };
        return new MoveResult { Caster = castingUnit, From = from, To = to };
    }
}