using AKidsDream.Units.Resources;
using AKidsDream.Common.Logging;
using Godot;
using Serilog;

namespace AKidsDream.Commands;

public class SelectAbilityCommand(
    Unit caster,
    StringName abilityName
) : IGameCommand
{
    public CommandResult Execute(GameContext context)
    {
        if (caster is null)
            return CommandResult.Fail(this, CommandFailureType.NullArgument, "No caster was provided.");

        var ability = caster.AbilityC.GetAbility(abilityName);

        if (ability is null)
            return CommandResult.Fail(this, CommandFailureType.NullArgument, $"Ability '{abilityName}' for '{caster.UnitName}' was not found.");
        
        context.AbilityVisualizer.ShowReachVisualization(
            caster,
            caster.TileLocation,
            ability
        );

        Log.ForContext<SelectAbilityCommand>().Here().Info(
            "Selected ability '{AbilityName}' for unit '{UnitName}' (id: {UnitId})",
            abilityName,
            caster.UnitName,
            caster.UnitId
        );

        return CommandResult.Ok(this);
    }
}