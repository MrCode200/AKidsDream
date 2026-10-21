#nullable enable
using AKidsDream.Utilities;
using TweenComp.Resources.Blueprint;
using AKidsDream.Utilities.Errors;
using AKidsDream.Utilities.Logging;
using AKidsDream.Utilities.Results;
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
    private static readonly ILogger Log = GameLogger.For<SelectAbilityCommand>();

    public Result<GameError> Execute(GameContext context)
    {
        if (!caster.AbilityComp.Abilities.TryGetValue(abilityName, out var ability))
            return Result<GameError>.Fail(new AbilityError.AbilityNotFound(caster.CasterId, abilityName));

        context.AbilityVisualizer.ShowReachVisualization(
            abilityContext,
            payload,
            ability
        );

        Log.ForContext<SelectAbilityCommand>()
            .ForContext("CasterIdTag", caster.CasterId)
            .ForContext("CasterNameTag", caster.CasterName)
            .Here().Info(
            "Selected ability '{AbilityName}' for unit '{UnitName}' (id: {UnitId})",
            abilityName,
            caster.UnitType,
            caster.UnitId
        );

        return Result<GameError>.Ok();
    }
}