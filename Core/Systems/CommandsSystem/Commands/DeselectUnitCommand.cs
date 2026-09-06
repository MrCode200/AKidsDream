#nullable enable
using AKidsDream.Common;
using AKidsDream.Common.Errors;
using AKidsDream.Common.Logging;
using AKidsDream.Common.Results;
using Serilog;

namespace AKidsDream.Commands;

public sealed class DeselectUnitCommand(Unit unit) : IGameCommand
{
    public Result<GameError> Execute(GameContext context)
    {
        Log.ForContext<DeselectUnitCommand>().Here().Info(
            "Deselected unit '{UnitName}' (id: {UnitId})",
            unit.UnitName,
            unit.UnitId);

        unit.SelectableComp.IsSelected = false;

        return Result<GameError>.Ok();
    }
}
