using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using AKidsDream.Managers.SaveSystems;
using AKidsDream.Common.Logging;
using AKidsDream.Core.Managers;
using AKidsDream.Core.Teams;
using AKidsDream.Managers.SaveSystem.Resources;
using Serilog;

namespace AKidsDream.Managers;

[GlobalClass]
public partial class GameLoopManager : Node
{
	public int CurrentRound;
	public PlayerId ActivePlayerId { get; private set; }
	private Dictionary<PlayerId, PlayerData> _playerTurnOrder = new();
	public Dictionary<PlayerId, PlayerData> PlayerTurnOrder() => new(_playerTurnOrder);
	
	private static readonly ILogger Log = GameLogger.For(typeof(GameLoopManager));
	private bool _stateLoaded;
	
	public void LoadState(GameStateData state)
	{
		CurrentRound = state.GameRound;
		_playerTurnOrder = state.PlayerTurnOrder;
		ActivePlayerId = new PlayerId(state.ActivePlayerIdInt);
		_stateLoaded = true; // Flag to skip default initialization in _Ready
		Log.Here().Info("Loaded game state: Round={CurrentRound}, ActivePlayer={ActivePlayerId}, PlayerCount={PlayerCount}", 
			state.GameRound, state.ActivePlayerIdInt, state.PlayerTurnOrder.Count);	}
	
	public override async void _Ready()
	{
		try
		{
			await ToSignal(EventBus.Instance, EventBus.SignalName.GameInitialized);
		
			if (!_stateLoaded)
			{
				// Default initialization for new games
				SetTurnOrder(GameManager.Instance.PlayerTeamRegistry.GetAllPlayers());
				ActivePlayerId = _playerTurnOrder.Keys.First();
				CurrentRound = 1;
			}
		
			EventBus.Instance.EmitSignal(EventBus.SignalName.NewRoundStarted, ActivePlayerId.Value, CurrentRound);

			Log.Here().Info("GameLoopManager initialized, starting Player is {ActivePlayerId}", ActivePlayerId);
			
			_playerTurnOrder[ActivePlayerId].Controller.StartTurn();
			EventBus.Instance.EmitSignal(EventBus.SignalName.TurnStarted, 
				ActivePlayerId.Value, CurrentRound);
		}
		catch (Exception e)
		{
			Log.Here().Error("A unexpected error occurred in GameLoopManager _Ready: {exception}", e);
		}
	}

	private void SetTurnOrder(PlayerData[] players)
	{
		_playerTurnOrder = players.ToDictionary(p => p.PlayerId, p => p);
	}

	/// <summary>
	/// Ends the turn for the specified player, if it's their turn.
	/// </summary>
	/// <param name="playerId">The ID of the player ending their turn.</param>
	/// <returns>True if the turn was successfully ended, false if the player is not the active player.</returns>
	public bool EndPlayerTurn(PlayerId playerId)
	{
		if (playerId != ActivePlayerId)
		{
			Log.Here().Warn("Player {PlayerId} tried to end turn, but it's not their turn", playerId);
			return false;
		}

		_playerTurnOrder[ActivePlayerId].Controller.EndTurn();
		EventBus.Instance.EmitSignal(EventBus.SignalName.TurnEnded, 
			ActivePlayerId.Value, CurrentRound);

		var idList = _playerTurnOrder.Keys.ToList();
		var nextPlayerId = idList[(idList.IndexOf(playerId) + 1) % idList.Count];
		ActivePlayerId = nextPlayerId;
		
		TryStartNewRound(nextPlayerId, idList.First());
		
		Log.Here().Info("Player {PlayerId} ended turn, starting {NextPlayerId}", playerId, nextPlayerId);

		_playerTurnOrder[ActivePlayerId].Controller.StartTurn();
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
}
