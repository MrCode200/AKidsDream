using Godot;

namespace AKidsDream.Components;

public partial class ActionComponent : Node
{
	[Export] public MoveComponent MoveC;
	[Export] public AttackComponent AttackC;
	[Export] public SelectableComponent SelectC;
	[Export] public int MaxMoveActions = 1;
	public int MoveActions { get; private set; }

	public override void _Ready()
	{
		// BUG:
		// Need to get MoveComponent from parent after ready, export assignment doesn't work
		// Thus is injections needed by Unit.cs
		AttackC = GetParent().GetNode<AttackComponent>("AttackComponent");
		SelectC = GetParent().GetNode<SelectableComponent>("SelectableComponent");
		// MoveC = GetParent().GetNode<MoveComponent>("MoveComponent");

		MoveActions = MaxMoveActions;
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