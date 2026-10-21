using AKidsDream.Abilities;
using AKidsDream.Util.Identifiers;
using Godot;

namespace AKidsDream.Entities.Cards;

[GlobalClass]
[Tool]
public partial class AbilityCardData : Resource
{
    [Export] public string CardId { get; set; } = "";
    [Export] public string OverrideDisplayName { get; set; } = "";
    [Export] public string Description { get; set; } = "";
    [Export] public Texture2D? OverrideIcon { get; set; }
    [Export] public AbilityData Ability;
    
    public Texture2D Icon => OverrideIcon ?? Ability.Icon;
    public string Name => string.IsNullOrEmpty(OverrideDisplayName) ? Ability.Name.ToString() : OverrideDisplayName;
}