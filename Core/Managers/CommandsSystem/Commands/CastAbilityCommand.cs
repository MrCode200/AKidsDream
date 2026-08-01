using AKidsDream.Common.Logging;
using AKidsDream.Units.Resources;
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
            return CommandResult.Fail(this, "No caster was provided.");
        
        if (targetTiles is null || targetTiles.Length == 0)
            return CommandResult.Fail(this, "No target tiles were provided.");

        bool success = caster.AbilityC.Cast(abilityName, targetTiles, context.Board);

        if (!success)
            return CommandResult.Fail(this, "Ability could not be cast");

        Log.ForContext<CastAbilityCommand>().Here().Info(
            "Casted ability '{AbilityName}' for unit '{UnitName}' (id: {UnitId}) at {TargetCount} target(s)",
            abilityName,
            caster.UnitName,
            caster.UnitId,
            targetTiles.Length);
        
        return CommandResult.Ok(this);
    }
}