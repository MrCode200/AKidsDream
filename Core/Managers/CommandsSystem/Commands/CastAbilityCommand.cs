using System.Threading.Tasks;
using AKidsDream.Common.Logging;
using AKidsDream.Common;
using AKidsDream.Common.Components;
using Godot;
using Serilog;

namespace AKidsDream.Commands;

public class CastAbilityBaseCommand(
    Unit caster,
    StringName abilityName,
    AbilityContext abilityContext,
    AbilityPayload payload
) : IAsyncGameBaseCommand
{
    public async Task<CommandResult> ExecuteAsync(GameContext context)
    {
        if (caster is null)
            return CommandResult.Fail(this, CommandFailureType.NullArgument, "No caster was provided.");

        if (payload.ProcessingTiles.Count == 0)
            return CommandResult.Fail(this, CommandFailureType.NullArgument, "No target tiles were provided.");

        var castResult =  await caster.AbilityC.CastAsync(abilityName, abilityContext, payload.AccumulatedTargets);

        if (!castResult.Success)
        {
            CommandFailureType failureType = castResult.FailureReason switch
            {
                CastFailureReason.AbilityNotFound => CommandFailureType.AbilityNotFound,
                CastFailureReason.CannotAfford => CommandFailureType.MissingAbilityPoints,
                CastFailureReason.TilesOutOfRange => CommandFailureType.InvalidTargetLocation,
                CastFailureReason.EffectExecutionFailed => CommandFailureType.EffectExecutionFailed,
                CastFailureReason.InvalidTargetsSelected => CommandFailureType.InvalidTargetsSelected,
                _ => CommandFailureType.Unknown
            };
            
            // NOTE:
            // one day maybe add metadata?
            // as different commands could contain info such as EffectResult which could become much to add to track in CommandResult...
            if (failureType is CommandFailureType.EffectExecutionFailed)
                return CommandResult.Fail(this, failureType, $"Effect Failed with: {castResult.EffectResult}"); 
                
            return CommandResult.Fail(this, failureType, $"Ability cast failed: {castResult.FailureReason}");
        }
        
        context.AbilityVisualizer.ClearTilemaps();

        Log.ForContext<CastAbilityBaseCommand>().Here().Info(
            "Casted ability '{AbilityName}' for unit '{UnitName}' (id: {UnitId}) at {TargetCount} target(s)",
            abilityName,
            caster.UnitName,
            caster.UnitId,
            payload.ProcessingTiles.Count);

        return CommandResult.Ok(this);
    }
}