using System.Collections.Generic;
using System.Linq;
using Godot;
using AKidsDream.Managers.SaveSystems;
using AKidsDream.Common.Logging;
using AKidsDream.Core.Managers;
using AKidsDream.Core.Teams;
using AKidsDream.Core.Controllers;
using Serilog;

namespace AKidsDream.Managers;

[GlobalClass]
public partial class GameLoopManager : Node
{
	[Export] public ControllerFactory ControllerFactory;

	public int CurrentTurn;
	private Dictionary<PlayerId, PlayerData> _turnOrder = new();
	private PlayerId _activePlayerId;
	private static readonly ILogger Log = GameLogger.For(typeof(GameLoopManager));

	public override void _Ready()
	{
		EventBus.Instance.GameInitialized += OnGameInitialized;
	}

	private void OnGameInitialized()
	{
		SetTurnOrder(GameManager.Instance.PlayerTeamRegistry.GetAllPlayers());
		_activePlayerId = _turnOrder.Keys.First();
		SendEventBusSignals(null, _activePlayerId);
		_turnOrder[_activePlayerId].Controller.StartTurn();
	}

	private void SetTurnOrder(PlayerData[] players)
	{
		_turnOrder = players.ToDictionary(p => p.PlayerId, p => p);
	}

	public void EndPlayerTurn(PlayerId playerId)
	{
		if (playerId != _activePlayerId)
		{
			Log.Here().Warn("Player {PlayerId} tried to end turn, but it's not their turn", playerId);
			return;
		}

		_turnOrder[_activePlayerId].Controller.EndTurn();

		var idList = _turnOrder.Keys.ToList();
		var nextPlayerId = idList[(idList.IndexOf(playerId) + 1) % idList.Count];
		_activePlayerId = nextPlayerId;

		SendEventBusSignals(playerId, nextPlayerId);

		_turnOrder[_activePlayerId].Controller.StartTurn();
	}

	private void SendEventBusSignals(PlayerId? oldPlayerId, PlayerId newPlayerId)
	{
		EventBus.Instance.EmitSignal(EventBus.SignalName.NewTurnStarted, newPlayerId.Value, CurrentTurn);
		if (newPlayerId == GameManager.Instance.LocalPlayerId)
			EventBus.Instance.EmitSignal(EventBus.SignalName.LocalPlayerTurnStarted, newPlayerId.Value, CurrentTurn);
		else if (oldPlayerId == GameManager.Instance.LocalPlayerId)
			EventBus.Instance.EmitSignal(EventBus.SignalName.LocalPlayerTurnEnded, newPlayerId.Value, CurrentTurn);
	}

	public override void _ExitTree()
	{
		EventBus.Instance.GameInitialized -= OnGameInitialized;
	}
}
