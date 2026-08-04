#nullable enable
using AKidsDream.Common.Logging;
using AKidsDream.GameBoard;
using AKidsDream.Units.Resources;
using Godot;
using Serilog;

namespace AKidsDream.Abilities.Effects;

[GlobalClass]
public partial class MoveSelfEffect : EffectData
{
    public override EffectResult ApplyEffect(Unit source, Board board, Vector2I[] targetTiles)
    {
        var tiles = GetAffectedTiles(targetTiles, board, source.OwnerId);
        if (tiles.Length != 1)
        {
            return new ErrorResult 
            { 
                Source = source, 
                Effect = this, 
                Error = $"MoveSelfEffect pattern returned multiple tiles: {tiles.Length}" 
            };        
        }
        
        Vector2I from = source.TileLocation;
        Vector2I to = tiles[0];
        source.Move(to);
        return new MoveResult { Source = source, From = from, To = to };
    }
}