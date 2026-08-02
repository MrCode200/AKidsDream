using AKidsDream.GameBoard;
using AKidsDream.Units.Resources;
using Godot;
using Godot.Collections;

namespace AKidsDream.Abilities.Effects;

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
            if (!board.TryGetUnitAt(tile, out var target)) continue;
            
            target.HealthC.Damage(Amount);
            results.Add(new DamageResult { Target = target, Tile = tile, Amount = Amount });
        }
        if (results.Count == 0) 
            return new CompositeResult { Results = [] };
        return results.Count > 1 ? new CompositeResult { Results = results } : results[0];
    }
}