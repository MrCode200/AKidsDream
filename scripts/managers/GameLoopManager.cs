using Godot;
using AKidsDream.Components;
using AKidsDream.Globals;
using AKidsDream.Units;

[GlobalClass]
public partial class GameLoopManager : Node
{
	public int CurrentTurn;

	public override void _Ready()
	{
		EventBus.Instance.BoardGenerated += () => { if (CurrentTurn == 0) StartTurn(); };
	}

	public void StartTurn()
	{
		// Update Actions 
		foreach (var node in GetTree().GetNodesInGroup(Global.Groups.PlayerUnits.GetFieldStringValue()))
		{
			var unit = (Unit)node;
			unit.ActionC.ResetActions();
		}
		CurrentTurn++;
		GD.Print($"New Turn {CurrentTurn} started");
	}
	
	public void EndPlayerTurn()
	{
		foreach (var node in GetTree().GetNodesInGroup(Global.Groups.PlayerUnits.GetFieldStringValue()))
		{
			var unit = (Unit)node;
			unit.ActionC.ToggleSelectComponent(false);
		}
		GD.Print($"Player Turn {CurrentTurn} ended");
	}
}
