using System;
using System.IO;
using System.Linq;
using AKidsDream.GameBoard;
using AKidsDream.Units.Resources;
using AKidsDream.Managers.SaveSystem.Resources;
using AKidsDream.Utilities;
using Godot;
using Godot.Collections;

namespace AKidsDream.Managers.SaveSystems;

public static class SaveLoadManager
{
	/// <summary>
	/// Loads the game state from a file.
	/// </summary>
	/// <param name="stateFileName">The filename to be loaded</param>
	/// <param name="board">The board, which gets used to init itself.</param>
	public static void LoadGameState(string stateFileName, Board board, Node entityLayer, bool removeExistingUnits = true)
	{
		if (removeExistingUnits)
		{
			foreach (var unit in entityLayer.GetChildren())
			{
				unit.QueueFree();
				GD.Print($"Removed existing unit {unit.Name}");
			}
		}
		
		var state = ResourceIO.Load<GameStateData>(Path.Combine(Global.SavePath, stateFileName)) ?? new GameStateData();

		GD.Print($"Initializing units from {stateFileName}");
		var initializedUnits = _initializeUnits(entityLayer, state.UnitStateResources);

		int highestId = initializedUnits.Select(unit => unit.UnitId).DefaultIfEmpty(0).Max();
		Utils.SetNextId(highestId + 1);
		GD.Print($"Set nextId to {highestId + 1}");

		board.Init(state.BoardStateData, initializedUnits);
	}

	/// <summary>
	/// Saves the current state of the board.
	/// </summary>
	/// <param name="board">The Board instance of the current game</param>
	/// <param name="saveFileName">The name of the save file.
	/// If null Generates: GameState + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")</param>
	/// <returns>True on success, else false.</returns>
	public static void SaveState(Board board, string saveFileName = null)
	{
		saveFileName ??= "GameState" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
		var state = new GameStateData();

		state.BoardStateData = board.StateData;
		// Iterate through BoardState tiles instead of scene tree
		foreach (var unit in board.GetAllUnits())
		{
			state.UnitStateResources.Add(
				UnitStateData.Create(unit)
			);
		}

		ResourceIO.Save(state, Path.Combine(Global.SavePath, saveFileName));
	}

	/// <summary>
	/// Initializes units from the saved <see cref="UnitStateData"/> data in the <see cref="GameStateData"/>.
	/// Loads unit scenes, adds them to the scene tree, and sets their initial state.
	/// <Returns>An array of initialized units.</Returns>
	/// </summary>
	private static Array<Unit> _initializeUnits(Node parent, Array<UnitStateData> initialUnits)
	{
		if (initialUnits == null) return [];
		
		Array<Unit> initializedUnits = [];
		foreach (var state in initialUnits)
		{
			var unitName = state.UnitName.ToString();
			var scenePath = $"res://Entities/Units/{unitName}/{unitName}.tscn";
			var unitScene = GD.Load<PackedScene>(scenePath);

			var newUnit = unitScene.Instantiate<Unit>();
			newUnit.Init(
				state.UnitName,
				state.Team,
				state.TileLocation,
				state.UnitStats,
				state.UnitId
			);

			// Set position and TileLocation disway to skip signal emitting from MoveC
			newUnit.Position = Board.TileToWorldPosition(state.TileLocation);

			initializedUnits.Add(newUnit);

			parent.AddChild(newUnit);
			GD.Print($"Initialized unit {state.UnitName} at {state.TileLocation} with Parent {parent.Name}");
		}

		return initializedUnits;
	}
}
