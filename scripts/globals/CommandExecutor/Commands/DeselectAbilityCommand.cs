using AKidsDream.Units;

namespace AKidsDream.Commands;

public class DeselectAbilityCommand(Unit caster) : IGameCommand
{
    public CommandResult Execute(GameContext context)
    {
        if (caster is null)
            return CommandResult.Fail(this, "No caster was provided.");

        context.AbilityVisualizer.ClearTilemaps();
        
        return CommandResult.Ok(this);
    }
}