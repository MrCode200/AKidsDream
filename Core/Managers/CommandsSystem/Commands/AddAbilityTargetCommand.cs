using System.Collections.Generic;
using System.Linq;
using AKidsDream.Common.Logging;
using AKidsDream.Units.Resources;
using AKidsDream.Units.Resources.Components;
using Godot;
using Serilog;

namespace AKidsDream.Commands;

public sealed class AddAbilityTargetBaseCommand(
    Unit caster,
    StringName abilityName,
    Vector2I targetedTile,
    AbilityContext abilityContext,
    AbilityPayload abilityPayload
) : IGameBaseCommand
{
    private static readonly ILogger Log = GameLogger.For(typeof(AddAbilityTargetBaseCommand));
    
    public CommandResult Execute(GameContext context)
    {

        if (caster is null)
            return CommandResult.Fail(this, CommandFailureType.NullArgument, "No caster was provided.");

        if (abilityName is null)
            return CommandResult.Fail(this, CommandFailureType.NullArgument, "No ability name was provided.");

        if (!caster.AbilityC.Abilities.TryGetValue(abilityName, out var ability))
            return CommandResult.Fail(this, CommandFailureType.AbilityNotFound, $"Ability '{abilityName}' not found.");
        
        var targetsToValidate = new List<Vector2I>(abilityPayload.AccumulatedTargets) { targetedTile };
// Use AbilityC.ValidateCast which handles batch vs sequential processing based on RunParallel flag
        if (!caster.AbilityC.ValidateCast(
                abilityName,
                abilityContext,
                targetsToValidate,
                out var payload,
                out var failureReason)
           )
        {
            return CommandResult.Fail(
                this,
                MapCastFailureToCommandFailure(failureReason),
                $"Validation failed: {failureReason}"
            );
        }

        abilityPayload.SetValuesTo(payload);

        Log.Here().Info(
            "Added target {TargetTile} for ability '{AbilityName}' for unit '{UnitName}' (id: {UnitId})",
            targetedTile,
            abilityName,
            caster.UnitName,
            caster.UnitId);
        
        // Show effect visualization for all effects
        context.AbilityVisualizer.ShowEffectVisualization(
            abilityContext,
            payload,
            ability.Effects
        );

        context.AbilityVisualizer.ShowReachVisualization(abilityContext, payload, ability);

        return CommandResult.Ok(this);
    }

    private static CommandFailureType MapCastFailureToCommandFailure(CastFailureReason reason)
    {
        return reason switch
        {
            CastFailureReason.AbilityNotFound => CommandFailureType.AbilityNotFound,
            CastFailureReason.InvalidTargetCount => CommandFailureType.InvalidTargetCount,
            CastFailureReason.TilesOutOfRange => CommandFailureType.InvalidTargetLocation,
            CastFailureReason.CannotAfford => CommandFailureType.MissingAbilityPoints,
            _ => CommandFailureType.Unknown
        };
    }
}