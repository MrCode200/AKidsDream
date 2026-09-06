#nullable enable
using System.Collections.Generic;
using System.Linq;
using AKidsDream.Common.Components.TweenComponent.Resources;
using AKidsDream.Common.Errors;
using AKidsDream.Common.Results;
using AKidsDream.Managers.SaveSystems;
using Godot;

namespace AKidsDream.Commands;

public class RmvAbilityTargetCommand(
    Vector2I targetedTile,
    AbilityContext ctx,
    AbilityPayload payload
) : BaseAbilityTargetCommand(targetedTile, ctx, payload)
{
    protected override Result<GameError> ValidatePreconditions()
    {
        int index = Payload.AccumulatedTargets.LastIndexOf(TargetedTile);
        if (index == -1)
            return Result<GameError>.Fail(new ValidationError.InvalidArgument(nameof(TargetedTile), $"Target '{TargetedTile}' not present in payload."));
        return Result<GameError>.Ok();
    }

    protected override List<Vector2I> GetModifiedTargets()
    {
        int index = Payload.AccumulatedTargets.LastIndexOf(TargetedTile);
        var previewAccumulatedTargets = Payload.AccumulatedTargets.ToList();
        previewAccumulatedTargets.RemoveAt(index);
        return previewAccumulatedTargets;
    }

    protected override string GetActionName()
    {
        return "Removed";
    }
}
