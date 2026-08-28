#nullable enable
using AKidsDream.Common.Logging;
using AKidsDream.GameBoard;
using AKidsDream.Common;
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
            // For chaining: update CurrentOrigin to the last target tile
            // This allows the next target selection to be relative to the new position
            case 0:
                return;
            case > 1:
                Log.Here().Debug("Passed more than one tile, only using last tile");
                break;
        }

        payload.CurrentOrigin = payload.ProcessingTiles[^1];
        
    }

    public override EffectResult ApplyEffect(AbilityContext context, AbilityPayload payload)
    {
        var tiles = GetAffectedTiles(context, payload);
        if (tiles.Length == 0)
        {
            return new ErrorResult
            {
                Source = context.Caster,
                Effect = this,
                Error = "MoveSelfEffect pattern returned no tiles"
            };
        }
        
        Vector2I from = context.Caster.TileLocation;
        Vector2I to = tiles[0];
        context.Caster.Move(to);
        return new MoveResult { Source = context.Caster, From = from, To = to };
    }
}