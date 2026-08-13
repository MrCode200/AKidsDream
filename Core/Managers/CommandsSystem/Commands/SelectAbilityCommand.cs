using AKidsDream.Units.Resources;
using AKidsDream.Units.Resources.Components;
using AKidsDream.Common.Logging;
using Godot;
using Serilog;

namespace AKidsDream.Commands;

public class SelectAbilityBaseCommand(
    Unit caster,
    StringName abilityName,
    AbilityContext abilityContext,
    AbilityPayload payload
) : IGameBaseCommand
{
    public CommandResult Execute(GameContext context)
    {
        if (caster is null)
            return CommandResult.Fail(this, CommandFailureType.NullArgument, "No caster was provided.");
        
        if (!caster.AbilityC.Abilities.TryGetValue(abilityName, out var ability))
            return CommandResult.Fail(this, CommandFailureType.NullArgument, $"Ability '{abilityName}' for '{caster.UnitName}' was not found.");
        
        context.AbilityVisualizer.ShowReachVisualization(
            abilityContext,
            payload,
            ability
        );

        Log.ForContext<SelectAbilityBaseCommand>().Here().Info(
            "Selected ability '{AbilityName}' for unit '{UnitName}' (id: {UnitId})",
            abilityName,
            caster.UnitName,
            caster.UnitId
        );

        return CommandResult.Ok(this);
    }
}