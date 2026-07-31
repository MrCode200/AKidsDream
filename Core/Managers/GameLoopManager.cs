using Godot;
using AKidsDream.Units.Resources.Components;
using AKidsDream.Managers.SaveSystems;
using AKidsDream.Units.Resources;

namespace AKidsDream.Managers;

[GlobalClass]
public partial class GameLoopManager : Node
{
	public int CurrentTurn;
	public bool PlayerPlayed;
	public bool EnemyPlayed;

	public override void _Ready()
	{
		EventBus.Instance.BoardGenerated += () => { if (CurrentTurn == 0) StartNewTurn(); };
	}

	public void StartNewTurn()
	{
		// Update Actions 
		foreach (var node in GetTree().GetNodesInGroup(nameof(Global.Groups.PlayerUnits)))
		{
			var unit = (Unit)node;
			unit.AbilityC.ResetPool();
		}
		CurrentTurn++;
		GD.Print($"New Turn {CurrentTurn} started");
	}
	
	public void EndPlayerTurn()
	{
		foreach (var node in GetTree().GetNodesInGroup(nameof(Global.Groups.PlayerUnits)))
		{
			var unit = (Unit)node;
		}

		PlayerPlayed = true;
		CheckIfToStartNewTurn();
		GD.Print($"Player Turn {CurrentTurn} ended");
	}

	public void EndEnemyTurn()
	{
		CheckIfToStartNewTurn();	
	}
	
	public void CheckIfToStartNewTurn()
	{
		if (PlayerPlayed && EnemyPlayed)
		{
			StartNewTurn();
		}
	}
}
