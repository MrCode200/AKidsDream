using AKidsDream.GameBoard;
using AKidsDream.Units;
using Godot;
using Godot.Collections;

namespace AKidsDream.Abilities;

[GlobalClass]
public partial class DamageEffect : EffectData
{
    [Export] public int Amount;

    public override EffectResult ApplyEffect(Unit source, Board board, Vector2I[] targetTile)
    {
        var tiles = GetAffectedTiles(targetTile, board);
        var results = new Array<EffectResult>();

        foreach (var tile in tiles)
        {
            var target = board.GetUnitAt(tile);
            if (target == null) continue;
            target.HealthC.Damage(Amount);
            results.Add(new DamageResult { Target = target, Tile = tile, Amount = Amount });
        }
        if (results.Count == 0) return null;
        return results.Count > 1 ? new CompositeResult { Results = results } : results[0];
    }
}