using AKidsDream.Common.Components;
using Godot;

namespace AKidsDream.Abilities.CostModifiers;

[GlobalClass]
public abstract partial class CostModifier : Resource
{
    public virtual bool CanReplenishPool() => false;
    public abstract int GetCost(int baseCost, AbilityContext context, AbilityPayload payload);
}