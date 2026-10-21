#nullable enable
using AKidsDream.Utilities;
using AKidsDream.Utilities.Errors;
using AKidsDream.Utilities.Logging;
using AKidsDream.Utilities.Results;
using Serilog;

namespace AKidsDream.Commands;

public sealed class DeselectUnitCommand(Unit unit) : IGameCommand
{
    private static readonly ILogger _log = GameLogger.For<DeselectUnitCommand>();

    public Result<GameError> Execute(GameContext context)
    {
        _log.ForContext<DeselectUnitCommand>().Here().Info(
            "Deselected unit '{UnitName}' (id: {UnitId})",
            unit.UnitType,
            unit.UnitId);

        unit.SelectableComp.IsSelected = false;

        return Result<GameError>.Ok();
    }
}
