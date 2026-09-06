using System.Collections.Generic;
using AKidsDream.Common.Components.TweenComponent.Resources;
using AKidsDream.Common.Errors;
using AKidsDream.Common.Results;
using Godot;

namespace AKidsDream.Commands;

public sealed class AddAbilityTargetCommand(
    Vector2I targetedTile,
    AbilityContext ctx,
    AbilityPayload payload
) : BaseAbilityTargetCommand(targetedTile, ctx, payload)
{
    protected override Result<GameError> ValidatePreconditions()
    {
        throw new System.NotImplementedException();
    }

    protected override List<Vector2I> GetModifiedTargets()
    {
        return new List<Vector2I>(Payload.AccumulatedTargets) { TargetedTile };
    }

    protected override string GetActionName()
    {
        return "Added";
    }
}