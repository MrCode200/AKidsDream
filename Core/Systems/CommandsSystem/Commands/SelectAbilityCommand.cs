#nullable enable
using AKidsDream.Common;
using AKidsDream.Common.Components.TweenComponent.Resources;
using AKidsDream.Common.Errors;
using AKidsDream.Common.Logging;
using AKidsDream.Common.Results;
using Godot;
using Serilog;

namespace AKidsDream.Commands;

public class SelectAbilityCommand(
    Unit caster,
    StringName abilityName,
    AbilityContext abilityContext,
    AbilityPayload payload
) : IGameCommand
{
    public Result<GameError> Execute(GameContext context)
    {
        if (!caster.AbilityC.Abilities.TryGetValue(abilityName, out var ability))
            return Result<GameError>.Fail(new AbilityError.AbilityNotFound(caster.CasterId, abilityName));

        context.AbilityVisualizer.ShowReachVisualization(
            abilityContext,
            payload,
            ability
        );

        Log.ForContext<SelectAbilityCommand>().Here().Info(
            "Selected ability '{AbilityName}' for unit '{UnitName}' (id: {UnitId})",
            abilityName,
            caster.UnitName,
            caster.UnitId
        );

        return Result<GameError>.Ok();
    }
}