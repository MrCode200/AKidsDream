using System;
using System.Linq;
using AKidsDream.GameBoard;
using AKidsDream.Globals;
using Godot;

namespace AKidsDream.Units.FieldAccessPatterns;

// NOTE:
// Avoid 1 << 31
// when adding more than just RelationShipFilters and different Enums (TargetTypeFilter, UnitTagFilter, etc.)
[Flags]
public enum TargetFilter
{
    EmptyTiles = 1 << 0,
    Friend = 1 << 1,
    Enemy = 1 << 2,

    AnyUnit = Friend | Enemy,
    AnyTile = EmptyTiles | Friend | Enemy,
}

[GlobalClass]
public abstract partial class AccessFieldPattern : Resource
{
    [Export] public TargetFilter AllowedTargets { get; set; } = TargetFilter.AnyTile;

    public Vector2I[] GetTiles(Vector2I origin, Board board)
    {
        var tiles = GetTilesUnfiltered(origin, board);
        if (tiles.Length == 0 || AllowedTargets == TargetFilter.AnyTile) return tiles;

        tiles = tiles.Where(tile =>
        {
            var tileData = board.GetTileAt(tile);

            if (AllowedTargets.HasFlag(TargetFilter.EmptyTiles) && tileData?.Unit == null)
            {
                return true;
            }

            if (AllowedTargets.HasFlag(TargetFilter.Friend) &&
                tileData?.Unit?.Team == Global.UnitTeam.Player)
            {
                return true;
            }

            if (AllowedTargets.HasFlag(TargetFilter.Enemy) &&
                tileData?.Unit?.Team == Global.UnitTeam.Enemy)
            {
                return true;
            }

            return false;
        }).ToArray();

        return tiles;
    }

    public abstract Vector2I[] GetTilesUnfiltered(Vector2I origin, Board board);
}