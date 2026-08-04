using System;
using System.Diagnostics.CodeAnalysis;
using AKidsDream.Common.Logging;
using AKidsDream.Core.Teams;
using AKidsDream.GameBoard;
using AKidsDream.Managers.SaveSystem.Resources;
using AKidsDream.Managers.SaveSystems;
using AKidsDream.Core.Controllers;
using Godot;
using Godot.Collections;
using Serilog;

namespace AKidsDream.Core.Managers;

// Upon Creation GameManager get a Payload of information on how to set up the game...
// The following is a placeholder

public class GameCreationPayload
{
    public readonly string LoadGameFileName = "DevSave.tres";
    public readonly int LocalPlayerId = 1;
    public readonly Vector2I BoardSize = new(9, 9);

    // NOTE make for Local Player ID 1, so that on fallback (local not set) it returns the correct player id
    public readonly Array<PlayerData> Players =
    [
        new PlayerData(
            new PlayerId(1),
            "MrMagic",
            new TeamId(1),
            Global.UnitColor.Blue,
            ControllerType.PlayerInteractionController
        ),
        new PlayerData(
            new PlayerId(2),
            "MrSorceress",
            new TeamId(2),
            Global.UnitColor.Red,
            ControllerType.PlayerInteractionController
        )
    ];

    public readonly Array<TeamData> Teams =
    [
        new TeamData(new TeamId(1)),
        new TeamData(new TeamId(2))
    ];

    public readonly System.Collections.Generic.Dictionary<(TeamId, TeamId), TeamRelation> TeamRelations = new()
    {
        { (new TeamId(1), new TeamId(2)), TeamRelation.Enemy },
    };
}

// 2. Implement, if need help go claude
// TODO: Make Turn Ending Command(yes very probably good idea?)
// TODO: Make its processing happen first, over all other nodes...

public partial class GameManager : Node2D
{
    [Export] public ControllerFactory ControllerFactory;
    private static GameManager _instance;
    private readonly ILogger _log = GameLogger.For<GameManager>();
    public readonly PlayerTeamRegistry PlayerTeamRegistry = new();
    public readonly TeamRelationResolver TeamRelationResolver = new();
    private PlayerId _localPlayerId;
    public PlayerId LocalPlayerId => _localPlayerId;

    public static GameManager Instance => _instance;

    [Export] public string LoadFileName = "OldSave.tres";
    [Export] public string SaveFileName = "NewSave.tres";
    [Export] public Node EntityLayer;

    /// <summary>
    /// If true, saves the board state when the board is removed from the scene tree.
    /// </summary>
    [Export] public bool SaveOnExit = true;

    [Export] public Board GameBoard;

    public override void _Ready()
    {
        _instance = this;

        var gameCreationPayload = new GameCreationPayload();
        // Prefer an explicit LoadFileName set in the inspector; fall back to the
        // creation payload's suggestion only if that's unset.
        var loadFileName = string.IsNullOrEmpty(LoadFileName)
            ? gameCreationPayload.LoadGameFileName
            : LoadFileName;

        if (!string.IsNullOrEmpty(loadFileName))
        {
            //TODO: make LoagGamestate return CreationPayload, as most Initialize Methods and signals are duplicates
            _log.Here().Info(
                "GameManager initializing with LoadFileName: '{LoadFileName}', SaveFileName: '{SaveFileName}'",
                loadFileName,
                SaveFileName);
            SaveLoadManager.LoadGameState(loadFileName, GameBoard, EntityLayer);
            EventBus.Instance.EmitSignal(EventBus.SignalName.GameInitialized);
            return;
        }

        // -- Create new game --
        _localPlayerId = new PlayerId(gameCreationPayload.LocalPlayerId);
        InitializeRegistries(gameCreationPayload.Players, gameCreationPayload.Teams, gameCreationPayload.TeamRelations);
        InitializeControllers(gameCreationPayload.Players);
        InitializeNewBoard(gameCreationPayload);
        EventBus.Instance.EmitSignal(EventBus.SignalName.GameInitialized);
    }

    public void InitializeControllers(Array<PlayerData> players)
    {
        foreach (var playerData in players)
        {
            var controller = ControllerFactory.CreateController(playerData);
            playerData.Controller = controller;
        }
    }

    public override void _ExitTree()
    {
        _instance = null;

        if (SaveOnExit)
        {
            _log.Here().Info(
                "GameManager exiting, saving game state to '{SaveFileName}'",
                SaveFileName);
            SaveLoadManager.SaveState(GameBoard, SaveFileName);
        }
        else
        {
            _log.Here().Info("GameManager exiting without saving (SaveOnExit: false)");
        }

        Log.CloseAndFlush();
        QueueFree();
    }

    // -- INITIALIZATION METHODS --

    public void InitializeRegistries(Array<PlayerData> players, Array<TeamData> teams,
        System.Collections.Generic.Dictionary<(TeamId, TeamId), TeamRelation> teamRelations)
    {
        foreach (var playerData in players)
            PlayerTeamRegistry.RegisterPlayer(playerData);

        foreach (var teamData in teams)
            PlayerTeamRegistry.RegisterTeam(teamData);

        _log.Here().Info("Registered {PlayerCount} players and {TeamCount} teams", players.Count, teams.Count);

        foreach (var (pair, relation) in teamRelations)
            TeamRelationResolver.SetRelation(pair.Item1, pair.Item2, relation);

        _log.Here().Info("Registered {TeamRelationCount} team relations", teamRelations.Count);
    }


    private void InitializeNewBoard(GameCreationPayload gameCreationPayload)
    {
        var newBoardState = new BoardStateData
        {
            Width = gameCreationPayload.BoardSize.X,
            Height = gameCreationPayload.BoardSize.Y
        };

        GameBoard.Init(newBoardState);
    }

    // -- HELPER METHODS --

    public bool IsLocalPlayer(PlayerId playerId) => playerId == _localPlayerId;

    public bool IsHostileToLocalPlayer(PlayerId targetPlayerId)
    {
        if (!TryGetLocalPlayerTeam(out var localTeamId))
        {
            _log.Here().Error("Local player not set, assuming hostile");
            return true;
        }

        return PlayerTeamRegistry.IsHostileToPlayer(targetPlayerId, _localPlayerId);
    }

    public bool TryGetLocalPlayerTeam([NotNullWhen(true)] out TeamId? teamId)
    {
        return PlayerTeamRegistry.TryGetPlayersTeamId(_localPlayerId, out teamId);
    }
}