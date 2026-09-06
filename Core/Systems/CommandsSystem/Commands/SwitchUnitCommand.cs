#nullable enable
using AKidsDream.Common;
using AKidsDream.Common.Errors;
using AKidsDream.Common.Logging;
using AKidsDream.Common.Results;
using AKidsDream.Managers.SaveSystems;
using Serilog;

namespace AKidsDream.Commands;

public sealed class SwitchUnitCommand(Unit oldUnit, Unit newUnit) : IGameCommand
{
    public Result<GameError> Execute(GameContext context)
    {
        oldUnit.SelectableComp.IsSelected = false;
        newUnit.SelectableComp.IsSelected = true;
        
        Log.ForContext<SwitchUnitCommand>().Here().Info(
            "Switched from unit '{OldUnitName}' (id: {OldUnitId}) to unit '{NewUnitName}' (id: {NewUnitId})",
            oldUnit.UnitName,
            oldUnit.UnitId,
            newUnit.UnitName,
            newUnit.UnitId);

        EventBus.Instance.EmitSignal(EventBus.SignalName.UnitChanged, oldUnit, newUnit);

        return Result<GameError>.Ok();
    }
}
