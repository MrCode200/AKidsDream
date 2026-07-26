using AKidsDream.GameBoard;
using AKidsDream.Units;
using Godot;
using Godot.Collections;

namespace AKidsDream.Abilities;

[GlobalClass]
public partial class DamageEffect : EffectData
{
    [Export] public int Amount;

    public override EffectResult Apply(Unit source, Board board, Vector2I[] targetTile)
    {
        var tiles = GetAffectedTiles(targetTile, board);
        var results = new Array<EffectResult>();

        foreach (var tile in tiles)
        {
            var target = Board.Instance.GetUnitAt(tile);
            if (target == null) continue;
            target.HealthC.Damage(Amount);
            results.Add(new DamageResult { Target = target, Tile = tile, Amount = Amount });
        }
        return new CompositeResult { Results = results };
    }
}