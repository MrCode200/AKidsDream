#nullable enable
using AKidsDream.Common.Components.TweenComponent.Resources;
using AKidsDream.Util.Identifiers;
using Godot;

namespace AKidsDream.Abilities;

public interface IAbilityCaster
{
    public IIdTag CasterId { get; }
    public string CasterName { get; } // Only Needed for Debugging (currently... may change in the future, based on new effects)
    
    public PlayerId OwnerId { get; }
    public Vector2I TileLocation { get; set; }
    
    // Components
    public AnimationComponent? AnimComp { get; }
    // public AbilityComponent? AbilityC { get; }
}