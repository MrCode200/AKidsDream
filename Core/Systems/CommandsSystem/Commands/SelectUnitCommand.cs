#nullable enable
using AKidsDream.Common;
using AKidsDream.Common.Errors;
using AKidsDream.Common.Logging;
using AKidsDream.Common.Results;
using Serilog;

namespace AKidsDream.Commands;

public sealed class SelectUnitCommand(Unit unit) : IGameCommand
{
    public Result<GameError> Execute(GameContext context)
    {
        Log.ForContext<SelectUnitCommand>().Here().Info(
            "Selected unit '{UnitName}' (id: {UnitId}) at {TileLocation}",
            unit.UnitName,
            unit.UnitId,
            unit.TileLocation);

        unit.SelectableComp.IsSelected = true;

        return Result<GameError>.Ok();
    }
}
