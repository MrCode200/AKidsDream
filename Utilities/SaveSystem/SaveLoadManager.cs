#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using AKidsDream.Utilities.Logging;
using AKidsDream.Core.Managers;
using AKidsDream.Core.Teams;
using AKidsDream.GameBoard;
using AKidsDream.Utilities;
using AKidsDream.Core.Managers.GameModeManager;
using AKidsDream.Managers.SaveSystem.Resources;
using AKidsDream.Managers.SaveSystems.Rehydration;
using Godot;
using Godot.Collections;
using Serilog;

namespace AKidsDream.Managers.SaveSystems;

/// <summary>
/// Coordinates loading and saving a full game state. This class only orchestrates
/// the sequence of steps — the actual work is delegated to focused collaborators:
/// <see cref="GameStateRepository"/> (disk I/O),
/// (scene-tree cleanup), and <see cref="UnitStateInitializer"/> (unit construction,
/// which in turn uses <see cref="UnitOwnershipResolver"/> for validation).
/// </summary>
public static class SaveLoadManager
{
    private static readonly ILogger Log = GameLogger.For(typeof(SaveLoadManager));

    /// <summary>
    /// Loads the game state from a file.
    /// </summary>
    /// <param name="stateFileName">The filename to be loaded</param>
    /// <returns>The loaded GameStateData</returns>
    public static GameStateData LoadGameState(string stateFileName)
    {
        Log.ForContext("StateFileName", stateFileName)
            .Here()
            .Debug("Loading game state from '{StateFileName}'", stateFileName);

        GameStateData state;
        try
        {
            state = GameStateRepository.Load(stateFileName);
        }
        catch (Exception ex)
        {
            Log.ForContext("StateFileName", stateFileName)
                .Here()
                .Fatal(ex, "Failed to load game state from '{StateFileName}'", stateFileName);
            throw;
        }

        Log.Here()
            .Debug("LoadGameState completed successfully");
        return state;
    }

    /// <summary>
    /// Saves the current state of the board.
    /// </summary>
    /// <param name="board">The Board instance of the current game</param>
    /// <param name="gameLoopManager">The GameLoopManager, which is used to get the current state.</param>
    /// <param name="saveFileName">The name of the save file.
    /// If null Generates: GameState + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")</param>
    /// <param name="playerTeamRegistry">The registry of players and teams</param>
    /// <param name="teamRelationResolver">The resolver of team relations</param>
    /// <returns>True on success, else false.</returns>
    public static void SaveState(Board board,
        PlayerTeamRegistry playerTeamRegistry,
        TeamRelationResolver teamRelationResolver,
        GameLoopManager gameLoopManager,
        GameModeLoader gameModeLoader,
        string? saveFileName = null
    )
    {
        saveFileName ??= "GameState" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
        Log.ForContext("SaveFileName", saveFileName)
            .Here().Debug("Saving game state to '{SaveFileName}'", saveFileName);
        
        var state = new GameStateData
        {
            GameMode = gameModeLoader.GetCurrentGameMode(),
            GameRound = gameLoopManager.CurrentRound,
            PlayerTurnOrder = gameLoopManager.PlayerTurnOrder(),
            ActivePlayerIdInt = gameLoopManager.ActivePlayerId.Value,
            PlayerData = new Array<PlayerData>(playerTeamRegistry.GetAllPlayers()),
            TeamData = new Array<TeamData>(playerTeamRegistry.GetAllTeams()),
            TeamRelations = teamRelationResolver.Relations,
            BoardState = board.State
        };

        // Iterate through BoardState tiles instead of scene tree
        foreach (var unit in board.GetAllUnits())
        {
            state.UnitStateResources.Add(UnitStateData.Create(unit));
        }

        GameStateRepository.Save(state, saveFileName);
        Log.Here().Info("Game state saved successfully to '{SaveFileName}' with {UnitCount} units",
            saveFileName, state.UnitStateResources.Count);
    }
}