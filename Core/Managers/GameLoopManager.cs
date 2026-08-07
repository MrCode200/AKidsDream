using System.Collections.Generic;
using System.Linq;
using Godot;
using AKidsDream.Managers.SaveSystems;
using AKidsDream.Common.Logging;
using AKidsDream.Core.Managers;
using AKidsDream.Core.Teams;
using Serilog;

namespace AKidsDream.Managers;

[GlobalClass]
public partial class GameLoopManager : Node
{
	public int CurrentRound;
	public PlayerId ActivePlayerId { get; private set; }
	private Dictionary<PlayerId, PlayerData> _turnOrder = new();
	private static readonly ILogger Log = GameLogger.For(typeof(GameLoopManager));

	public override void _Ready()
	{
		EventBus.Instance.GameInitialized += OnGameInitialized;
	}

	private void OnGameInitialized()
	{
		SetTurnOrder(GameManager.Instance.PlayerTeamRegistry.GetAllPlayers());
		ActivePlayerId = _turnOrder.Keys.First();
		CurrentRound = 1;
		Log.Here().Info("GameLoopManager initialized, starting Player is {ActivePlayerId}", ActivePlayerId);
		_turnOrder[ActivePlayerId].Controller.StartTurn();
		EventBus.Instance.EmitSignal(EventBus.SignalName.TurnStarted, 
			ActivePlayerId.Value, CurrentRound);
	}

	private void SetTurnOrder(PlayerData[] players)
	{
		_turnOrder = players.ToDictionary(p => p.PlayerId, p => p);
	}

	public bool EndPlayerTurn(PlayerId playerId)
	{
		if (playerId != ActivePlayerId)
		{
			Log.Here().Warn("Player {PlayerId} tried to end turn, but it's not their turn", playerId);
			return false;
		}

		_turnOrder[ActivePlayerId].Controller.EndTurn();
		EventBus.Instance.EmitSignal(EventBus.SignalName.TurnEnded, 
			ActivePlayerId.Value, CurrentRound);

		var idList = _turnOrder.Keys.ToList();
		var nextPlayerId = idList[(idList.IndexOf(playerId) + 1) % idList.Count];
		ActivePlayerId = nextPlayerId;
		
		TryStartNewRound(nextPlayerId, idList.First());
		
		Log.Here().Info("Player {PlayerId} ended turn, starting {NextPlayerId}", playerId, nextPlayerId);

		_turnOrder[ActivePlayerId].Controller.StartTurn();
		EventBus.Instance.EmitSignal(EventBus.SignalName.TurnStarted, 
			ActivePlayerId.Value, CurrentRound);
		return true;
	}
	
	private void TryStartNewRound(PlayerId nextPlayerId, PlayerId firstPlayer)
	{
		// Increment round when we cycle back to the first player
		if (nextPlayerId == firstPlayer)
		{
			CurrentRound++;
		
			Log.Here().Info("Starting new round {RoundNumber}", CurrentRound);
			
			EventBus.Instance.EmitSignal(EventBus.SignalName.NewRoundStarted, 
				nextPlayerId.Value, CurrentRound);
		}
	}

	public override void _ExitTree()
	{
		EventBus.Instance.GameInitialized -= OnGameInitialized;
	}
}
