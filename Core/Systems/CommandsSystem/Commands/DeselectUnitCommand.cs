#nullable enable
using AKidsDream.Managers.SaveSystems;
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
            unit.UnitTypeName,
            unit.UnitId);

        unit.SelectableComp.IsSelected = false;
        EventBus.Instance.EmitSignal(EventBus.SignalName.UnitDeselected, unit);

        return Result<GameError>.Ok();
    }
}
