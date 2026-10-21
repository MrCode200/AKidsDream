#nullable enable
using AKidsDream.Utilities;
using AKidsDream.Utilities.Errors;
using AKidsDream.Utilities.Logging;
using AKidsDream.Utilities.Results;
using Serilog;

namespace AKidsDream.Commands;

public sealed class SelectUnitCommand(Unit unit) : IGameCommand
{
    public Result<GameError> Execute(GameContext context)
    {
        Log.ForContext<SelectUnitCommand>().Here().Info(
            "Selected unit '{UnitName}' (id: {UnitId}) at {TileLocation}",
            unit.UnitType,
            unit.UnitId,
            unit.TileLocation);

        unit.SelectableComp.IsSelected = true;

        return Result<GameError>.Ok();
    }
}
