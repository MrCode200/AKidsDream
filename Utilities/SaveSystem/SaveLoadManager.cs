#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using AKidsDream.Common.Logging;
using AKidsDream.Core.Managers;
using AKidsDream.Core.Teams;
using AKidsDream.GameBoard;
using AKidsDream.Common;
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
    /// <param name="board">The board, which gets used to init itself.</param>
    /// <param name="gameLoopManager">The GameLoopManager, which gets used to init itself.</param>
    /// <param name="entityLayer">To where the child Unit Nodes should be added to</param>
    /// <param name="gameManager"></param>
    public static void LoadGameState(
        string stateFileName,
        Board board,
        GameLoopManager gameLoopManager,
        Node entityLayer,
        GameManager gameManager
    )
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

        gameManager.InitializeRegistries(state.PlayerData, state.TeamData, state.TeamRelations);
        gameManager.InitializeControllers(state.PlayerData);

        gameLoopManager.LoadState(state);

        AssignNextIds(state.UnitStateResources!, state.PlayerData, state.TeamData);
        var initializedUnits =
            UnitStateInitializer.InitializeUnits(
                entityLayer,
                state.UnitStateResources,
                gameManager.PlayerTeamRegistry,
                board
            );
        board.Init(state.BoardStateData, initializedUnits);

        Log.Here()
            .Debug("LoadGameState completed successfully");
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
        string? saveFileName = null
    )
    {
        saveFileName ??= "GameState" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
        var state = new GameStateData
        {
            GameRound = gameLoopManager.CurrentRound,
            PlayerTurnOrder = gameLoopManager.PlayerTurnOrder(),
            ActivePlayerIdInt = gameLoopManager.ActivePlayerId.Value,
            PlayerData = new Array<PlayerData>(playerTeamRegistry.GetAllPlayers()),
            TeamData = new Array<TeamData>(playerTeamRegistry.GetAllTeams()),
            TeamRelations = teamRelationResolver.Relations,
            BoardStateData = board.StateData
        };

        // Iterate through BoardState tiles instead of scene tree
        foreach (var unit in board.GetAllUnits())
        {
            state.UnitStateResources.Add(UnitStateData.Create(unit));
        }

        GameStateRepository.Save(state, saveFileName);
    }

    private static void AssignNextIds(IEnumerable<UnitStateData> units, IEnumerable<PlayerData> players,
        IEnumerable<TeamData> teams)
    {
        var highestUnitId = units.Select(u => u.UnitId).DefaultIfEmpty(1).Max();
        UnitId.SetNextId(highestUnitId + 1);

        var highestPlayerId = players.Select(p => p.PlayerId.Value).DefaultIfEmpty(1).Max();
        PlayerId.SetNextId(highestPlayerId + 1);

        var highestTeamId = teams.Select(t => t.TeamId.Value).DefaultIfEmpty(1).Max();
        TeamId.SetNextId(highestTeamId + 1);


        Log.ForContext("HighestUnitId", highestUnitId)
            .ForContext("HighestPlayerId", highestPlayerId)
            .ForContext("HighestTeamId", highestTeamId)
            .Here()
            .Debug(
                "Set nextId for UnitId to {HighestUnitId}, PlayerId to {HighestPlayerId}, TeamId to {HighestTeamId}",
                highestUnitId + 1, highestPlayerId + 1, highestTeamId + 1
            );
    }
}