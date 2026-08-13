using AKidsDream.Common.Logging;
using AKidsDream.Units.Resources;
using Serilog;

namespace AKidsDream.Commands;

public class DeselectAbilityBaseCommand(Unit caster) : IGameBaseCommand
{
    public CommandResult Execute(GameContext context)
    {
        if (caster is null)
            return CommandResult.Fail(this, CommandFailureType.NullArgument, "No caster was provided.");

        Log.ForContext<DeselectAbilityBaseCommand>().Here().Info(
            "Deselected ability for unit '{UnitName}' (id: {UnitId})",
            caster.UnitName,
            caster.UnitId);

        context.AbilityVisualizer.ClearTilemaps();
        
        return CommandResult.Ok(this);
    }
}