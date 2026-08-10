using AKidsDream.Common.Logging;
using AKidsDream.Units.Resources;
using AKidsDream.Units.Resources.Components;
using Godot;
using Serilog;

namespace AKidsDream.Commands;

public class CastAbilityCommand(
    Unit caster,
    StringName abilityName,
    Vector2I[] targetTiles
) : IGameCommand
{
    public CommandResult Execute(GameContext context)
    {
        if (caster is null)
            return CommandResult.Fail(this, CommandFailureType.NullArgument, "No caster was provided.");

        if (targetTiles is null || targetTiles.Length == 0)
            return CommandResult.Fail(this, CommandFailureType.NullArgument, "No target tiles were provided.");

        var castResult = caster.AbilityC.Cast(abilityName, targetTiles, context.Board);

        if (!castResult.Success)
        {
            CommandFailureType failureType = castResult.FailureReason switch
            {
                CastFailureReason.AbilityNotFound => CommandFailureType.AbilityNotFound,
                CastFailureReason.CannotAfford => CommandFailureType.MissingAbilityPoints,
                CastFailureReason.TilesOutOfRange => CommandFailureType.InvalidTargetLocation,
                CastFailureReason.EffectExecutionFailed => CommandFailureType.EffectExecutionFailed,
                _ => CommandFailureType.Unknown
            };
            
            // NOTE:
            // one day maybe add metadata?
            // as different commands could contain info such as EffectResult which could become much to add to track in CommandResult...
            if (failureType is CommandFailureType.EffectExecutionFailed)
                return CommandResult.Fail(this, failureType, $"Effect Failed with: {castResult.EffectResult}"); 
                
            return CommandResult.Fail(this, failureType, $"Ability cast failed: {castResult.FailureReason}");
        }

        Log.ForContext<CastAbilityCommand>().Here().Info(
            "Casted ability '{AbilityName}' for unit '{UnitName}' (id: {UnitId}) at {TargetCount} target(s)",
            abilityName,
            caster.UnitName,
            caster.UnitId,
            targetTiles.Length);

        return CommandResult.Ok(this);
    }
}