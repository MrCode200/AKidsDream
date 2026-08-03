using System;
using System.Linq;
using AKidsDream.Core.Managers;
using AKidsDream.Managers;
using AKidsDream.Managers.SaveSystem.Resources;
using AKidsDream.GameBoard;
using AKidsDream.Managers.SaveSystems;
using AKidsDream.Units.Resources;
using Godot;

namespace AKidsDream.Abilities.Effects;

// NOTE:
// Avoid 1 << 31
// when adding more than just RelationShipFilters and different Enums (TargetTypeFilter, UnitTagFilter, etc.)
[Flags]
public enum TargetFilter
{
    EmptyTiles = 1 << 0,
    
    OwnedUnits = 1 << 1,
    Friendly = 1 << 2,
    Hostile = 1 << 3,

    AnyUnit = Friendly | Hostile,
    AnyTile = EmptyTiles | Friendly | Hostile,
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
            if(!board.TryGetTileAt(tile, out var tileData)) return false;

            if (AllowedTargets.HasFlag(TargetFilter.EmptyTiles) && tileData.Unit == null)
            {
                return true;
            }
            
            if (AllowedTargets.HasFlag(TargetFilter.OwnedUnits) &&
                tileData.Unit is not null &&
                GameManager.Instance.IsLocalPlayer(tileData.Unit.OwnerId))
            {
                return true;
            }
            
            if (AllowedTargets.HasFlag(TargetFilter.Friendly) &&
                tileData.Unit is not null &&
                !GameManager.Instance.IsHostileToLocalPlayer(tileData.Unit.TeamId))
            {
                return true;
            }

            if (AllowedTargets.HasFlag(TargetFilter.Hostile) &&
                tileData.Unit is not null &&
                GameManager.Instance.IsHostileToLocalPlayer(tileData.Unit.TeamId))
            {
                return true;
            }

            return false;
        }).ToArray();

        return tiles;
    }

    public abstract Vector2I[] GetTilesUnfiltered(Vector2I origin, Board board);
}