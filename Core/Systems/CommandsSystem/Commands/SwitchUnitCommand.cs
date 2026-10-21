#nullable enable
using AKidsDream.Utilities;
using AKidsDream.Utilities.Errors;
using AKidsDream.Utilities.Logging;
using AKidsDream.Utilities.Results;
using AKidsDream.Managers.SaveSystems;
using Serilog;

namespace AKidsDream.Commands;

public sealed class SwitchUnitCommand(Unit oldUnit, Unit newUnit) : IGameCommand
{
    private static readonly ILogger _log = GameLogger.For<SwitchUnitCommand>();

    public Result<GameError> Execute(GameContext context)
    {
        oldUnit.SelectableComp.IsSelected = false;
        newUnit.SelectableComp.IsSelected = true;
        
        _log.ForContext<SwitchUnitCommand>().Here().Info(
            "Switched from unit '{OldUnitName}' (id: {OldUnitId}) to unit '{NewUnitName}' (id: {NewUnitId})",
            oldUnit.UnitType,
            oldUnit.UnitId,
            newUnit.UnitType,
            newUnit.UnitId);

        EventBus.Instance.EmitSignal(EventBus.SignalName.UnitChanged, oldUnit, newUnit);

        return Result<GameError>.Ok();
    }
}
