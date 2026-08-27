using AKidsDream.Common;
using AKidsDream.Common.Components;
using AKidsDream.Common.Logging;
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

        Log.ForContext<SelectAbilityCommand>().Here().Info(
            "Selected ability '{AbilityName}' for unit '{UnitName}' (id: {UnitId})",
            abilityName,
            caster.UnitName,
            caster.UnitId
        );

        return CommandResult.Ok(this);
    }
}