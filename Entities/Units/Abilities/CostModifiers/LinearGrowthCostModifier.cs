using System;
using AKidsDream.Common.Components;
using Godot;

namespace AKidsDream.Abilities.CostModifiers;

[GlobalClass]
[Tool]
public partial class LinearGrowthCostModifier : CostModifier
{
    [Export] public int Growth = 1;
    [Export] public bool IgnoreFirstTarget = true;
    public override bool CanReplenishPool() { return Growth < 0; }

    public override int GetCost(int baseCost, AbilityContext context, AbilityPayload payload)
    {
        var growthMult = Math.Max(payload.AccumulatedTargets.Count - (IgnoreFirstTarget ? 1 : 0), 0);
        return baseCost + Growth * growthMult;
    }
}