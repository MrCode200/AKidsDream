#nullable enable
using AKidsDream.Abilities;
using AKidsDream.Util.Identifiers;
using Godot;

namespace AKidsDream.Core.Managers;

public sealed class CardCaster(IIdTag casterId, PlayerId ownerId, string casterName)
    : IAbilityCaster
{
    public IIdTag CasterId { get; } = casterId;
    public string CasterName { get; } = casterName;
    public PlayerId OwnerId { get; } = ownerId;
    public Vector2I TileLocation { get; set; } = new(-1, -1);
    public AnimationComponent? AnimComp { get; } = null;
}