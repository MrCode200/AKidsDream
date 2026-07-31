using AKidsDream.Units.Resources;

namespace AKidsDream.Commands;

public sealed class SelectUnitCommand(Unit unit) : IGameCommand
{
    public CommandResult Execute(GameContext context)
    {
        if (unit is null)
            return CommandResult.Fail(this, "Unit is null");
        
        unit.SelectableC.IsSelected = true;
        
        return CommandResult.Ok(this);
    }
}