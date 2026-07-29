using AKidsDream.Units;

namespace AKidsDream.Commands;

public sealed class DeselectUnitCommand(Unit unit) : IGameCommand
{
    public CommandResult Execute(GameContext context)
    {
        if (unit is null)
            return CommandResult.Fail(this, "Unit is null");
        
        unit.SelectableC.IsSelected = false;
        
        return CommandResult.Ok(this);
    }
}