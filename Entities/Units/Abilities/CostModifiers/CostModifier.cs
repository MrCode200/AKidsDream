using AKidsDream.Units.Resources.Components;
using Godot;

namespace AKidsDream.Abilities.CostModifiers;

[GlobalClass]
public abstract partial class CostModifier : Resource
{
    public abstract int GetCost(int baseCost, AbilityContext context, AbilityPayload payload);
}