/*using Godot;

namespace AKidsDream.Graveyard;

public partial class ActionComponent : Node
{
	[Export] public MoveComponent MoveC;
	[Export] public AttackComponent AttackC;
	[Export] public SelectableComponent SelectC;
	[Export] public int MaxMoveActions = 1;
	public int MoveActions { get; private set; }

	public override void _Ready()
	{
		MoveActions = MaxMoveActions;
		CallDeferred(nameof(_Initialize));
	}
	
	private void _Initialize()
	{
		MoveC.UnitMoved += (unit, oldTile, newTile) => { TakeMoveAction(); };
	}
	
	public bool TakeMoveAction()
	{
		GD.Print("TakeMoveAction");
		if (MoveActions > 0)
		{
			MoveActions -= 1;
			if (MoveActions == 0) ToggleSelectComponent(false);
			return true;
		}
		
		return false;
	}
		
	public void ResetActions()
	{
		MoveActions = MaxMoveActions;
		ToggleSelectComponent(true);
	}

	public void ToggleSelectComponent(bool enable)
	{
		if (enable)
		{
			SelectC.SetProcessMode(ProcessModeEnum.Inherit);
		}
		else
		{
			SelectC.SetProcessMode(ProcessModeEnum.Disabled);
		}
	}
}
*/