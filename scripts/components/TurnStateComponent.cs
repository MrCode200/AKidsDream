using Godot;

namespace AKidsDream.Components;

[GlobalClass]
public partial class TurnStateComponent : Node
{
	[Export] public MoveComponent MoveC;
	[Export] public SelectableComponent SelectC;
	[Export] public int MaxMoveActions = 2;
	public int MoveActions { get; private set; }

	public override void _Ready()
	{
		// BUG: Need to get MoveComponent from parent after ready, export assignment doesn't work
		//MoveC = GetParent().GetNode<MoveComponent>("MoveComponent");
		GD.Print(MoveC);
		MoveActions = MaxMoveActions;
		MoveC.BodyMoved += (body, oldTile, newTile) => { TakeMoveAction(); };
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