using System.Collections.Generic;
using System.IO;
using System.Linq;
using AKidsDream.Utilities.Logging;
using AKidsDream.Core.Teams;
using AKidsDream.GameBoard;
using AKidsDream.Managers.SaveSystem.Resources;
using AKidsDream.Managers.SaveSystems;
using AKidsDream.Managers.SaveSystems.Rehydration;
using AKidsDream.Util.Identifiers;
using AKidsDream.Util.Identifiers.Commands;
using AKidsDream.Managers;
using AKidsDream.Commands;
using AKidsDream.Utilities;
using AKidsDream.Core.Content;
using AKidsDream.Core.Managers.GameModeManager;
using AKidsDream.Core.Managers.ModManager;
using AKidsDream.Entities.Cards;
using AKidsDream.Tests;
using Godot;
using Godot.Collections;
using Serilog;

namespace AKidsDream.Core.Managers;

// Placeholder to create testingSave for new game at any position

// Upon Creation GameManager get a Payload of information on how to set up the game...
// The following is a placeholder
public class GameCreationPayload
{
	public readonly string LoadGameFileName = "NewSave.tres";
	public readonly string GameMode = "KingSlayer";
	public readonly int LocalPlayerId = 1;
	public readonly Vector2I BoardSize = new(14, 9);

	public readonly Array<PlayerData> Players =
	[
		new PlayerData(
			new PlayerId(1),
			"MrMagic",
			new TeamId(1),
			Global.UnitColor.Blue,
			"core:player_interaction_controller",
			5
		),
		new PlayerData(
			new PlayerId(2),
			"MrSorceress",
			new TeamId(2),
			Global.UnitColor.Red,
			"core:player_interaction_controller",
			5
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

	public static string testingFile = """
									   [gd_resource type="Resource" format=3]

									   [ext_resource type="Script" path="res://Entities/Board/Resources/BoardState.cs" id="1_vp3i7"]
									   [ext_resource type="Script" path="res://Core/Systems/TeamSystem/Resources/PlayerData.cs" id="2_ucnw2"]
									   [ext_resource type="Script" path="res://Core/Systems/TeamSystem/Resources/TeamData.cs" id="3_4n8g7"]
									   [ext_resource type="Script" path="res://Utilities/SaveSystem/Resources/UnitStateData.cs" id="4_dm7om"]
									   [ext_resource type="Script" path="res://Entities/Units/Resources/UnitStatsData.cs" id="5_jsx6i"]
									   [ext_resource type="Script" path="res://Utilities/SaveSystem/Resources/GameStateData.cs" id="6_nptp8"]
									   
									   [sub_resource type="Resource" id="Resource_e3xci"]
									   script = ExtResource("1_vp3i7")
									   Width = 12

									   [sub_resource type="Resource" id="Resource_r6a1v"]
									   script = ExtResource("2_ucnw2")
									   PlayerIdInt = 1
									   Name = "MrMagic"
									   TeamIdInt = 1
									   ControllerKey = "core:player_interaction_controller"
									   Mana = 10

									   [sub_resource type="Resource" id="Resource_ojjb5"]
									   script = ExtResource("2_ucnw2")
									   PlayerIdInt = 2
									   Name = "MrSorceress"
									   TeamIdInt = 2
									   ControllerKey = "core:player_interaction_controller"
									   UnitColor = 1
									   Mana = 10

									   [sub_resource type="Resource" id="Resource_xurp1"]
									   script = ExtResource("3_4n8g7")
									   TeamIdInt = 1

									   [sub_resource type="Resource" id="Resource_lnyo5"]
									   script = ExtResource("3_4n8g7")
									   TeamIdInt = 2

									   [sub_resource type="Resource" id="Resource_vp3i7"]
									   script = ExtResource("5_jsx6i")
									   MaxHealth = 2
									   Health = 2
									   metadata/_custom_type_script = "uid://8ujfv88riuka"

									   [sub_resource type="Resource" id="Resource_ucnw2"]
									   script = ExtResource("4_dm7om")
									   UnitId = 1
									   OwnerId = 1
									   UnitKey = "core:soldier"
									   TileLocation = Vector2i(5, 6)
									   UnitStats = SubResource("Resource_vp3i7")

									   [sub_resource type="Resource" id="Resource_4n8g7"]
									   script = ExtResource("5_jsx6i")
									   MaxHealth = 2
									   Health = 2
									   metadata/_custom_type_script = "uid://8ujfv88riuka"

									   [sub_resource type="Resource" id="Resource_dm7om"]
									   script = ExtResource("4_dm7om")
									   UnitId = 2
									   OwnerId = 2
									   UnitKey = "core:soldier"
									   TileLocation = Vector2i(4, 5)
									   UnitStats = SubResource("Resource_4n8g7")

									   [sub_resource type="Resource" id="Resource_jsx6i"]
									   script = ExtResource("5_jsx6i")
									   MaxHealth = 2
									   Health = 2
									   metadata/_custom_type_script = "uid://8ujfv88riuka"

									   [sub_resource type="Resource" id="Resource_nptp8"]
									   script = ExtResource("4_dm7om")
									   UnitId = 3
									   OwnerId = 1
									   UnitKey = "core:soldier"
									   TileLocation = Vector2i(6, 6)
									   UnitStats = SubResource("Resource_jsx6i")

									   [resource]
									   script = ExtResource("6_nptp8")
									   GameMode = "KingSlayer"
									   GameRound = 3
									   _playerTurnOrder = Dictionary[int, ExtResource("2_ucnw2")]({
									   1: SubResource("Resource_r6a1v"),
									   2: SubResource("Resource_ojjb5")
									   })
									   ActivePlayerIdInt = 1
									   BoardStateData = SubResource("Resource_e3xci")
									   PlayerData = Array[ExtResource("2_ucnw2")]([SubResource("Resource_r6a1v"), SubResource("Resource_ojjb5")])
									   TeamData = Array[ExtResource("3_4n8g7")]([SubResource("Resource_xurp1"), SubResource("Resource_lnyo5")])
									   _teamRelations = Dictionary[Vector2i, int]({
									   Vector2i(1, 2): 1,
									   Vector2i(2, 1): 1
									   })
									   UnitStateResources = Array[ExtResource("4_dm7om")]([SubResource("Resource_ucnw2"), SubResource("Resource_dm7om"), SubResource("Resource_nptp8")])

									   """;
}

// TODO:
// Make its processing happen first, over all other nodes... as it may be called through Instance etc...
// SOLUTION: use await ToSignal() in _Ready() (not ideal or good enough?)

// TODO: Make OnAbilitySelectedState handle failure of cast (what to do with tiles...) (stay selected, later add back key/btn to ability tiles?)

// CHECK: if commands DeselectAbilityCommand and SelectAbilityCommand are needed? (as can be bug as DeselectUnitCommand takes Unit)

// NEXT:
// TODO: on ability selected, if no tile exist to select return error (don't let it become selected in the first place)
// TODO: make pool bar stretch automatically with animation based on label size (numbers)
public partial class GameManager : Node2D
{
	[ExportCategory("Override LoadSave")] [Export]
	public string LoadFileName = "OldSave.tres";

	[Export] public string SaveFileName = "NewSave.tres";
    [Export] public bool SaveOnExit = true;
    [Export] public bool RemoveExistingUnits = true;

	[ExportGroup("Context")] [Export] public ControllerFactory ControllerFactory;
    [Export] public Board GameBoard;
    [Export] public CommandExecutor CommandExecutor;
    [Export] public AbilityVisualizer AbilityVisualizer;
    [Export] public ConsoleManager ConsoleManager;
    [Export] public Node EntityLayer;
    [Export] public GameLoopManager GameLoopManager;
    [Export] public CardManager CardManager;
    [Export] public PlayerHand PlayerHand;
    [Export] public GameModeLoader GameModeLoader;


    private readonly ILogger _log = GameLogger.For<GameManager>();
    public PlayerTeamRegistry PlayerTeamRegistry { get; set; }
    public readonly TeamRelationResolver TeamRelationResolver = new();
    public PlayerId LocalPlayerId { get; private set; }
    private GameContext _gameContext;
    
    /// <summary>
    /// If true, saves the board state when the board is removed from the scene tree.
    /// </summary>
    public override void _Ready()
    {
        _log.Here().Info("GameManager _Ready starting initialization");

        // Create the unified GameContext once
        PlayerTeamRegistry = new PlayerTeamRegistry(TeamRelationResolver);

        _gameContext = new GameContext(
            this,
            PlayerTeamRegistry,
            TeamRelationResolver,
            GameBoard,
            EventBus.Instance,
            GameLoopManager,
            AbilityVisualizer,
            EntityLayer,
            CommandExecutor,
            PlayerHand
        );

        // Phase 1: Initialize core systems (both load and new game paths)
        InitializeCoreSystems();

        var gameCreationPayload = new GameCreationPayload();
        // Prefer an explicit LoadFileName set in the inspector; fall back to the
        // creation payload's suggestion only if that's unset.
        var loadFileName = string.IsNullOrEmpty(LoadFileName)
            ? gameCreationPayload.LoadGameFileName
            : LoadFileName;

        if (RemoveExistingUnits) // DEV purpose only
        {
            var unitsRemovedCount = EntityLayer.GetChildCount();
            foreach (var unit in EntityLayer.GetChildren())
            {
                unit.QueueFree();
            }

            _log.Here()
                .Debug(
					"Removed {UnitCount} existing units from '{EntityLayer}'",
                    unitsRemovedCount,
                    EntityLayer.Name);
        }

        if (!string.IsNullOrEmpty(loadFileName))
        {
            // Phase 2: Load existing game
            _log.Here().Info(
				"GameManager initializing with LoadFileName: '{LoadFileName}', SaveFileName: '{SaveFileName}'",
                loadFileName,
                SaveFileName);

            // NOTE: delete later the 2/4 following lines (force creates save file for testing)
            // TODO: move the verification for existing directory into SaveLoadManager(?) or how to make sure that those folders exist..
            var savePath = Path.Combine(Global.SavePath, gameCreationPayload.LoadGameFileName);
            var directory = Path.GetDirectoryName(savePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
                _log.Here().Debug("Created save directory: {Directory}", directory);
            }
            File.WriteAllText(savePath, GameCreationPayload.testingFile);

            LoadGame(loadFileName);
            EventBus.Instance.EmitSignal(EventBus.SignalName.GameInitialized);
            return;
        }

        // Phase 2: Create new game
        _log.Here().Info("Creating new game with LocalPlayerId: {LocalPlayerId}", gameCreationPayload.LocalPlayerId);
        LocalPlayerId = new PlayerId(gameCreationPayload.LocalPlayerId);
        CreateNewGame(gameCreationPayload);
        EventBus.Instance.EmitSignal(EventBus.SignalName.GameInitialized);
        _log.Here().Info("New game created and initialized successfully");
    }

    private void InitializeCoreSystems()
    {
        _log.Here().Debug("Initializing core systems with GameContext");

        // Load mods (content registration phase)
        var modLoader = new ModLoader(_gameContext);
        var loadResult = modLoader.Load();
        if (loadResult.IsFailure)
        {
            _log.Here().Err("Failed to complete mod loading lifecycle: {Error}", loadResult.Error?.Message);
        }

        // Initialize systems with the context
        CommandExecutor.Init(_gameContext);
        ControllerFactory.Init(_gameContext);
        ConsoleManager.Init(_gameContext);
        GameLoopManager.Init(PlayerTeamRegistry);
        CardManager.Init(_gameContext);

        _log.Here().Debug("Core systems initialized successfully");
    }

    private void LoadGame(string loadFileName)
    {
        _log.Here().Info("Loading game from file: '{LoadFileName}'", loadFileName);

        // Load game state data
        var state = SaveLoadManager.LoadGameState(loadFileName);

        InitializeRegistries(state.PlayerData, state.TeamData, state.TeamRelations);
        InitializeControllers(state.PlayerData);

        GameLoopManager.LoadState(state);
        GameModeLoader.Init(state.GameMode, _gameContext);

        // Assign next IDs from loaded data
        AssignNextIds(state.UnitStateResources!, state.PlayerData, state.TeamData);

        // Initialize units from loaded data
        var initializedUnits =
            UnitStateInitializer.InitializeUnits(
                EntityLayer,
                state.UnitStateResources,
                PlayerTeamRegistry,
                GameBoard
            );

        // Initialize board with loaded state and units
        GameBoard.Init(state.BoardState, initializedUnits);

        _log.Here().Info("Game loaded successfully");
    }

    private void CreateNewGame(GameCreationPayload gcp)
    {
        _log.Here().Debug("Creating new game");

        InitializeRegistries(gcp.Players, gcp.Teams, gcp.TeamRelations);
        InitializeNewBoard(gcp);
        InitializeControllers(gcp.Players);
        GameModeLoader.Init(gcp.GameMode, _gameContext);

        _log.Here().Debug("New game created successfully");
    }

    private void AssignNextIds(IEnumerable<UnitStateData> units, IEnumerable<PlayerData> players,
        IEnumerable<TeamData> teams)
    {
        var highestUnitId = units.Select(u => u.UnitId).DefaultIfEmpty(1).Max();
        UnitId.SetNextId(highestUnitId + 1);

        var highestPlayerId = players.Select(p => p.PlayerId.Value).DefaultIfEmpty(1).Max();
        PlayerId.SetNextId(highestPlayerId + 1);

        var highestTeamId = teams.Select(t => t.TeamId.Value).DefaultIfEmpty(1).Max();
        TeamId.SetNextId(highestTeamId + 1);

        _log.Here().Debug(
            "Set nextId for UnitId to {HighestUnitId}, PlayerId to {HighestPlayerId}, TeamId to {HighestTeamId}",
            highestUnitId + 1, highestPlayerId + 1, highestTeamId + 1
        );
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
        if (SaveOnExit)
        {
            _log.Here().Info(
				"GameManager exiting, saving game state to '{SaveFileName}'",
                SaveFileName);
            SaveLoadManager.SaveState(GameBoard, PlayerTeamRegistry, TeamRelationResolver, GameLoopManager,
                GameModeLoader, SaveFileName);
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
        _log.Here().Debug("Initializing new board with size: {Width}x{Height}",
            gameCreationPayload.BoardSize.X, gameCreationPayload.BoardSize.Y);
        var newBoardState = new BoardState
        {
            Width = gameCreationPayload.BoardSize.X,
            Height = gameCreationPayload.BoardSize.Y
        };

        GameBoard.Init(newBoardState);
        _log.Here().Debug("New board initialized successfully");
    }

    // -- HELPER METHODS --
    /*
    // CHECK:
    // should not be needed local player management(?)
    // remove if not needed all methods (see above) and rmv from GameStateData...
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
    */
}
