using AKidsDream.Units.Resources;
using Godot;

namespace AKidsDream.Commands;

public class SelectAbilityCommand(
    Unit caster,
    StringName abilityName
) : IGameCommand
{
    public CommandResult Execute(GameContext context)
    {
        if (caster is null)
            return CommandResult.Fail(this, "No caster was provided.");

        var ability = caster.AbilityC.GetAbility(abilityName);

        if (ability is null)
            return CommandResult.Fail(this, $"Ability '{abilityName}' for '{caster.UnitName}' was not found.");

        context.AbilityVisualizer.ShowReachVisualization(
            caster,
            caster.TileLocation,
            ability
        );
        
        return CommandResult.Ok(this);
    }
}