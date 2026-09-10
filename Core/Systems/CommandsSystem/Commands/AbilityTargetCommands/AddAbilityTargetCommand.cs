using System.Collections.Generic;
using AKidsDream.Common.Components.TweenComponent.Resources;
using Godot;

namespace AKidsDream.Commands;

public sealed class AddAbilityTargetCommand(
    Vector2I targetedTile,
    AbilityContext ctx,
    AbilityPayload payload
) : BaseAbilityTargetCommand(targetedTile, ctx, payload)
{
    protected override List<Vector2I> GetModifiedTargets()
    {
        return new List<Vector2I>(Payload.AccumulatedTargets) { TargetedTile };
    }

    protected override string GetActionName()
    {
        return "Added";
    }
}