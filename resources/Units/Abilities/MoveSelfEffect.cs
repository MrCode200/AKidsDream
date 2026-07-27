using AKidsDream.GameBoard;
using AKidsDream.Units;
using Godot;

namespace AKidsDream.Abilities;

[GlobalClass]
public partial class MoveSelfEffect : EffectData
{
    public override EffectResult ApplyEffect(Unit source, Board board, Vector2I[] targetTiles)
    {
        var tiles = GetAffectedTiles(targetTiles, board);
        if (tiles.Length != 1)
        {
            GD.PrintErr("MoveSelfEffect: More than one tile affected by the pattern");
            return null;
        }
        
        Vector2I from = source.TileLocation;
        Vector2I to = tiles[0];
        source.Move(to);
        return new MoveResult { Source = source, From = from, To = to };
    }
}