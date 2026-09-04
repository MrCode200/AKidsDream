using System;
using System.Linq;
using AKidsDream.Core.Managers;
using AKidsDream.GameBoard;
using Godot;

namespace AKidsDream.Abilities.Effects;

// NOTE:
// Avoid 1 << 31
// when adding more than just RelationShipFilters and different Enums (TargetTypeFilter, UnitTagFilter, etc.)

/// <summary>
/// Defines filtering criteria for ability target selection on the game board.
/// </summary>
/// <remarks>
/// This enum uses the <see cref="FlagsAttribute"/> to allow combining multiple filters.
/// <para><b>Important:</b> When adding more filters beyond relationship types, avoid using bit 31 (1 &lt;&lt; 31)
/// to prevent overflow issues with the flags system.</para>
/// </remarks>
[Flags]
public enum TargetFilter
{
    
    /// <summary>
    /// Targets any tile on the board, including empty tiles and tiles with units.
    /// </summary>
    AnyTile = EmptyTiles | Friendly | Hostile,
    
    /// <summary>
    /// Targets empty tiles on the board (tiles without any unit).
    /// </summary>
    EmptyTiles = 1 << 0,

    /// <summary>
    /// Targets units owned by the local player.
    /// </summary>
    /// <remarks>
    /// This is a subset of <see cref="Friendly"/> targets, as owned units are always friendly.
    /// </remarks>
    OwnedUnits = 1 << 1,

    /// <summary>
    /// Targets all friendly units, including both owned units and allied units.
    /// </summary>
    Friendly = 1 << 2,

    /// <summary>
    /// Targets hostile (enemy) units.
    /// </summary>
    Hostile = 1 << 3,

    /// <summary>
    /// Targets any unit, regardless of relationship (friendly or hostile).
    /// </summary>
    AnyUnit = Friendly | Hostile,
}

[GlobalClass]
public abstract partial class AccessFieldPattern : Resource
{
    [Export] public TargetFilter AllowedTargets { get; set; } = TargetFilter.EmptyTiles;

    public Vector2I[] GetTiles(Vector2I origin, Board board, PlayerId playerCasterId, PlayerTeamRegistry playerTeamRegistry)
    {
        var tiles = GetTilesUnfiltered(origin, board);
        if (tiles.Length == 0 || AllowedTargets == TargetFilter.AnyTile) return tiles;

        tiles = tiles.Where(tile =>
        {
            if(!board.TryGetTileAt(tile, out var tileData)) return false;

            if (AllowedTargets.HasFlag(TargetFilter.EmptyTiles) && tileData.Unit == null)
            {
                return true;
            }
            
            if (AllowedTargets.HasFlag(TargetFilter.OwnedUnits) &&
                tileData.Unit is not null &&
                tileData.Unit.OwnerId == playerCasterId)
            {
                return true;
            }
            
            if (AllowedTargets.HasFlag(TargetFilter.Friendly) &&
                tileData.Unit is not null &&
                !playerTeamRegistry.IsHostileToPlayer(playerCasterId, tileData.Unit.OwnerId))
            {
                return true;
            }

            if (AllowedTargets.HasFlag(TargetFilter.Hostile) &&
                tileData.Unit is not null &&
                playerTeamRegistry.IsHostileToPlayer(playerCasterId, tileData.Unit.OwnerId))
            {
                return true;
            }

            return false;
        }).ToArray();

        return tiles;
    }

    public abstract Vector2I[] GetTilesUnfiltered(Vector2I origin, Board board);
}