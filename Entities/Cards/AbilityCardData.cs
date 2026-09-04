using AKidsDream.Abilities;
using Godot;

namespace AKidsDream.Entities.Cards;

[GlobalClass]
[Tool]
public partial class AbilityCardData : Resource
{
    [Export(PropertyHint.Range, "0,100")] public int SpawnDelay;
    [Export] public AbilityData Ability;
    
    public string Name => Ability.Name.ToString();
}