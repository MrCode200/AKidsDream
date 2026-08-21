using System.IO;
using AKidsDream.Common.Logging;
using AKidsDream.Core.Teams;
using AKidsDream.GameBoard;
using AKidsDream.Managers.SaveSystem.Resources;
using AKidsDream.Managers.SaveSystems;
using AKidsDream.Core.Controllers;
using AKidsDream.Managers;
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
	public readonly int LocalPlayerId = 1;
	public readonly Vector2I BoardSize = new(14, 9);

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

	public static string testingFile = """
	                                   [gd_resource type="Resource" format=3]
	                                   
	                                   [ext_resource type="Script" path="res://Entities/Board/Resources/BoardStateData.cs" id="1_vp3i7"]
	                                   [ext_resource type="Script" path="res://Core/Managers/TeamSystem/Resources/PlayerData.cs" id="2_ucnw2"]
	                                   [ext_resource type="Script" path="res://Core/Managers/TeamSystem/Resources/TeamData.cs" id="3_4n8g7"]
	                                   [ext_resource type="Script" path="res://Utilities/SaveSystem/Resources/UnitStateData.cs" id="4_dm7om"]
	                                   [ext_resource type="Script" path="res://Entities/Units/Resources/UnitStatsData.cs" id="5_jsx6i"]
	                                   [ext_resource type="Script" path="res://Utilities/SaveSystem/Resources/GameStateData.cs" id="6_nptp8"]
	                                   
	                                   [sub_resource type="Resource" id="Resource_e3xci"]
	                                   script = ExtResource("1_vp3i7")
	                                   Width = 12
	                                   
	                                   [sub_resource type="Resource" id="Resource_r6a1v"]
	                                   script = ExtResource("2_ucnw2")
	                                   PlayerIdInt = 1
	                                   PlayerName = "MrMagic"
	                                   TeamIdInt = 1
	                                   
	                                   [sub_resource type="Resource" id="Resource_ojjb5"]
	                                   script = ExtResource("2_ucnw2")
	                                   PlayerIdInt = 2
	                                   PlayerName = "MrSorceress"
	                                   TeamIdInt = 2
	                                   UnitColor = 1
	                                   
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
	                                   UnitName = 1
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
	                                   UnitName = 1
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
	                                   TileLocation = Vector2i(6, 6)
	                                   UnitStats = SubResource("Resource_jsx6i")
	                                   
	                                   [resource]
	                                   script = ExtResource("6_nptp8")
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
// TODO:
// make modular tweenComponent, update abilityButton to use its animation when visibility changed, or ready
// Use Visibility changed signal to trigger animation, 
// how to make sure for ready that the tweenComponent is faster (set as child for node which to tween?)

// TODO: on ability selected, if no tile exist to select return error (don't let it become selected in the first place)
// TODO: make pool bar stretch automatically with animation based on label size (numbers)
public partial class GameManager : Node2D
{
	[Export] public ControllerFactory ControllerFactory;
	private readonly ILogger _log = GameLogger.For<GameManager>();
	public readonly PlayerTeamRegistry PlayerTeamRegistry = new();
	public readonly TeamRelationResolver TeamRelationResolver = new();
	public PlayerId LocalPlayerId { get; private set; }

	public static GameManager Instance { get; private set; }

	[Export] public string LoadFileName = "OldSave.tres";
	[Export] public string SaveFileName = "NewSave.tres";
	[Export] public Node EntityLayer;
	[Export] public GameLoopManager GameLoopManager;

	/// <summary>
	/// If true, saves the board state when the board is removed from the scene tree.
	/// </summary>
	[Export] public bool SaveOnExit = true;

	[Export] public bool RemoveExistingUnits = true;

	[Export] public Board GameBoard;

	public override void _Ready()
	{
		Instance = this;

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
			//TODO: make LoagGamestate return CreationPayload, as most Initialize Methods and signals are duplicates
			_log.Here().Info(
				"GameManager initializing with LoadFileName: '{LoadFileName}', SaveFileName: '{SaveFileName}'",
				loadFileName,
				SaveFileName);
			
			// NOTE: delete later the 2 following lines (force creates save file for testing)
			// TODO: move the verification for existing directory into SaveLoadManager(?) or how to make sure that those folders exist..
			var savePath = Path.Combine(Global.SavePath, gameCreationPayload.LoadGameFileName);
			var directory = Path.GetDirectoryName(savePath);
			if (!string.IsNullOrEmpty(directory))
			{
				Directory.CreateDirectory(directory);
			}
			File.WriteAllText(savePath, GameCreationPayload.testingFile);
			
			SaveLoadManager.LoadGameState(loadFileName, GameBoard, GameLoopManager, EntityLayer);
			EventBus.Instance.EmitSignal(EventBus.SignalName.GameInitialized);
			return;
		}

		// -- Create new game --
		LocalPlayerId = new PlayerId(gameCreationPayload.LocalPlayerId);
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
		if (SaveOnExit)
		{
			_log.Here().Info(
				"GameManager exiting, saving game state to '{SaveFileName}'",
				SaveFileName);
			SaveLoadManager.SaveState(GameBoard, GameLoopManager, SaveFileName);
		}
		else
		{
			_log.Here().Info("GameManager exiting without saving (SaveOnExit: false)");
		}

		Instance = null;
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
