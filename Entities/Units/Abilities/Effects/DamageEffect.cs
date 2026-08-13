using System.Collections.Generic;
using System.Linq;
using AKidsDream.Units.Resources.Components;
using Godot;

namespace AKidsDream.Abilities.Effects;

[GlobalClass]
[Tool]
public partial class DamageEffect : EffectData
{
    [Export] public int Amount;
    
    public override EffectResult ApplyEffect(AbilityContext context, AbilityPayload payload)
    {
        var tiles = GetAffectedTiles(context, payload);
        List<EffectResult> results = [];

        foreach (var tile in tiles)
        {
            if (!context.Board.TryGetUnitAt(tile, out var target)) continue;
            
            target.HealthC.Damage(Amount);
            results.Add(new DamageResult { Target = target, Tile = tile, Amount = Amount });
        }
        if (results.Count == 0) 
            return new CompositeResult { Results = [] };
        return results.Count > 1 ? new CompositeResult { Results = [.. results] } : results[0];
    }
}