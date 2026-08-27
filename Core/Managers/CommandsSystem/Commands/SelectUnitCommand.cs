using AKidsDream.Common.Logging;
using AKidsDream.Common;
using Serilog;

namespace AKidsDream.Commands;

public sealed class SelectUnitCommand(Unit unit) : IGameCommand
{
    public CommandResult Execute(GameContext context)
    {
        if (unit is null)
            return CommandResult.Fail(this, CommandFailureType.NullArgument, "Unit is null");

        Log.ForContext<SelectUnitCommand>().Here().Info(
            "Selected unit '{UnitName}' (id: {UnitId}) at {TileLocation}",
            unit.UnitName,
            unit.UnitId,
            unit.TileLocation);

        unit.SelectableC.IsSelected = true;
        
        return CommandResult.Ok(this);
    }
}