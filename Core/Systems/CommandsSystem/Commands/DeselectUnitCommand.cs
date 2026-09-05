#nullable enable
using AKidsDream.Common;
using AKidsDream.Common.Errors;
using AKidsDream.Common.Logging;
using Serilog;

namespace AKidsDream.Commands;

public sealed class DeselectUnitCommand(Unit unit) : IGameCommand
{
    public CommandResult Execute(GameContext context)
    {
        if (unit is null)
            return CommandResult.Fail(this, new CommandError.NullArgument(nameof(unit), "Unit is null"));

        Log.ForContext<DeselectUnitCommand>().Here().Info(
            "Deselected unit '{UnitName}' (id: {UnitId})",
            unit.UnitName,
            unit.UnitId);

        unit.SelectableComp.IsSelected = false;

        return CommandResult.Ok(this);
    }
}
