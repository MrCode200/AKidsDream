using System;
using System.Linq;
using AKidsDream.GameBoard;
using AKidsDream.Globals;
using Godot;
using AKidsDream.Units;
using AKidsDream.Units.FieldAccessPatterns;

namespace AKidsDream.Abilities;

[GlobalClass]
public abstract partial class EffectData : Resource
{
    [Export] public AccessFieldPattern EffectPattern;
    [Export] public Global.AtlasCoordsSprite EffectAtlasCoords;

    protected Vector2I[] GetAffectedTiles(Vector2I[] targetTiles, Board board)
    {
        if (EffectPattern == null)
        {
            GD.PrintErr("EffectPattern is null");
            return [];
        }
        return targetTiles
            .SelectMany(tile => EffectPattern.GetTiles(tile, board))
            .ToArray();
    }
    
    public (Vector2I atlasCoord, Vector2I[] tiles) GetEffectVisualizationData(Unit source, Board board, Vector2I[] targetTiles)
    {
        var tiles = GetAffectedTiles(targetTiles, board);
        return (Global.AtlasCoordsSpriteVectors[EffectAtlasCoords], tiles);
    }

    public abstract EffectResult Apply(Unit source, Board board, Vector2I[] targetTiles);
}