using AKidsDream.Common.Logging;
using AKidsDream.Units.Resources;
using Serilog;

namespace AKidsDream.Commands;

public sealed class DeselectUnitCommand(Unit unit) : IGameCommand
{
    public CommandResult Execute(GameContext context)
    {
        if (unit is null)
            return CommandResult.Fail(this, "Unit is null");

        Log.ForContext<DeselectAbilityCommand>().Here().Info(
            "Deselected unit '{UnitName}' (id: {UnitId})",
            unit.UnitName,
            unit.UnitId);

        unit.SelectableC.IsSelected = false;
        
        return CommandResult.Ok(this);
    }
}