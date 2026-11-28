using System.Linq;
using AKidsDream.GameBoard;
using Godot;
using Godot.Collections;

namespace AKidsDream.Abilities.Effects;

public partial class CompositePattern : AccessFieldPattern
{
    // NOTE: Filters apply only to the 
    // TODO: instead of passing arrays and modifying them, rather pass list and return array at the end
    [Export] Array<AccessFieldPattern> Patterns { get; set; }

    public override Vector2I[] GetTiles(Vector2I? origin, Board board, PlayerId playerCasterId,
        PlayerTeamRegistry playerTeamRegistry)
    {
        if (OverrideToPlayerOrigin)
            origin = playerTeamRegistry.GetPlayer(playerCasterId).OwnedOriginTile;

        var combinedTiles = Patterns
            .SelectMany(p => p.GetTiles(origin, board, playerCasterId, playerTeamRegistry))
            .ToArray();
        
        if (combinedTiles.Length == 0 || AllowedTargets == TargetFilter.AnyTile) return combinedTiles;

        return FilterTiles(combinedTiles, board, playerCasterId, playerTeamRegistry);
    }

    public override Vector2I[] GetTilesUnfiltered(Vector2I? origin, Board board)
    {
        throw new System.NotImplementedException();
    }
}