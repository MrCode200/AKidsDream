using AKidsDream.Common.Logging;
using AKidsDream.Common;
using AKidsDream.Managers.SaveSystems;
using Serilog;

namespace AKidsDream.Commands;

public sealed class SwitchUnitCommand(Unit oldUnit, Unit newUnit) : IGameCommand
{
    public CommandResult Execute(GameContext context)
    {
        if (oldUnit is null || newUnit is null)
            return CommandResult.Fail(this, CommandFailureType.NullArgument, "Unit is null");

        Log.ForContext<SwitchUnitCommand>().Here().Info(
            "Switching from unit '{OldUnitName}' (id: {OldUnitId}) to unit '{NewUnitName}' (id: {NewUnitId})",
            oldUnit.UnitName,
            oldUnit.UnitId,
            newUnit.UnitName,
            newUnit.UnitId);

        oldUnit.SelectableComp.IsSelected = false;
        newUnit.SelectableComp.IsSelected = true;

        EventBus.Instance.EmitSignal(EventBus.SignalName.UnitChanged, oldUnit, newUnit);

        return CommandResult.Ok(this);
    }
}
