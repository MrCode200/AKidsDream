using AKidsDream.Units;
using Godot;

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

        bool success = caster.AbilityC.Cast(abilityName, targetTiles);

        if (!success)
            return CommandResult.Fail(this, "Ability could not be cast");
        
        return CommandResult.Ok(this);
    }
}