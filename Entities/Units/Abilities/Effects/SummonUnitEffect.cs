using System.Collections.Generic;
using AKidsDream.Common;
using AKidsDream.Common.Components.TweenComponent.Resources;
using Godot;

namespace AKidsDream.Abilities.Effects;

[GlobalClass]
[Tool]
public partial class SummonUnitEffect : EffectData
{
    [Export] public PackedScene SummonedUnit;
    [Export] public UnitStatsData SummonedUnitStats;

    public override EffectResult ApplyEffect(AbilityContext context, AbilityPayload payload, Vector2I[] affectedTiles)
    {
        var results = new List<EffectResult>();
        foreach (var tile in affectedTiles)
        {
            if (!context.GameContext.PlayerTeamRegistry.TryGetPlayer(context.Caster.OwnerId, out var playerData))
            {
                return new ErrorResult
                {
                    Caster = context.Caster,
                    Effect = this,
                    Error = "PlayerData using caster.OwnerID not found"
                };
            }

            var summoned = SummonedUnit.Instantiate<Unit>();
            summoned.Init(
                playerData,
                tile,
                SummonedUnitStats,
                context.GameContext.Board
            );
            context.GameContext.EntityLayer.AddChild(summoned);
            
            results.Add(new SummonedEntityResult
            {
                Caster = context.Caster,
                Summoned = summoned
            });
        }
        
        return new CompositeResult { Results = [.. results] };
    }
}