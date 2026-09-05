#nullable enable
using AKidsDream.Common;
using AKidsDream.Common.Errors;
using AKidsDream.Common.Logging;
using Serilog;

namespace AKidsDream.Commands;

public sealed class SelectUnitCommand(Unit unit) : IGameCommand
{
    public CommandResult Execute(GameContext context)
    {
        if (unit is null)
            return CommandResult.Fail(this, new CommandError.NullArgument(nameof(unit), "Unit is null"));

        Log.ForContext<SelectUnitCommand>().Here().Info(
            "Selected unit '{UnitName}' (id: {UnitId}) at {TileLocation}",
            unit.UnitName,
            unit.UnitId,
            unit.TileLocation);

        unit.SelectableComp.IsSelected = true;

        return CommandResult.Ok(this);
    }
}
