using Godot;
using AKidsDream.Managers.SaveSystems;
using AKidsDream.Units.Resources;
using AKidsDream.Common.Logging;
using Serilog;

namespace AKidsDream.Managers;

[GlobalClass]
public partial class GameLoopManager : Node
{
	private readonly ILogger _log = GameLogger.For<GameLoopManager>();

	public int CurrentTurn;
	public bool PlayerPlayed;
	public bool EnemyPlayed;
	// public bool PlayersTurn;

	public override void _Ready()
	{
		EventBus.Instance.EndTurnBtnPressed += EndPlayerTurn;
		EventBus.Instance.BoardGenerated += () =>
		{
			if (CurrentTurn == 0)
			{
				_log.Here().Info("Board generated, starting first turn if CurrentTurn is 0");
				StartNewTurn();
			}
		};
	}

	public void StartNewTurn()
	{
		// Update Actions 
		_log.Here().Info("Resetting ability pools"); 
		// CHECK:
		// if it should become a command,
		// will create clutter, though if command logs itself
		// Probably only if I ever add Ability which allows the resetting of ability pools else redundant
		// (? as only called here function reset)
		foreach (var node in GetTree().GetNodesInGroup(nameof(Global.Groups.PlayerUnits)))
		{
			var unit = (Unit)node;
			unit.AbilityC.ResetPool();
		}
		PlayerPlayed = false;
		EnemyPlayed = false;

		CurrentTurn++;
		_log.Here().Info("Turn {TurnNumber} started", CurrentTurn);
		EventBus.Instance.EmitSignal(EventBus.SignalName.NewTurnStarted, CurrentTurn);
	}

	public void EndPlayerTurn()
	{
		PlayerPlayed = true;
		_log.Here().Info("Player turn {TurnNumber} ended", CurrentTurn);
		CheckIfToStartNewTurn();
		EventBus.Instance.EmitSignal(EventBus.SignalName.PlayerTurnEnded);
	}

	public void EndEnemyTurn()
	{
		_log.Here().Info("Enemy turn {TurnNumber} ended", CurrentTurn);
		CheckIfToStartNewTurn();
		EventBus.Instance.EmitSignal(EventBus.SignalName.EnemyTurnEnded);
	}

	private void CheckIfToStartNewTurn()
	{
		if (PlayerPlayed && EnemyPlayed)
		{
			StartNewTurn();
		}
	}
}
