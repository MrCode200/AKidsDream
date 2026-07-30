using System;
using System.IO;
using System.Linq;
using AKidsDream.GameBoard;
using AKidsDream.resources.stateResources;
using AKidsDream.Units;
using Godot;
using Godot.Collections;

namespace AKidsDream.Globals;

public static class SaveLoadManager
{
    /// <summary>
    /// Loads the game state from a file.
    /// </summary>
    /// <param name="stateFileName">The filename to be loaded</param>
    /// <param name="board">The board, which gets used to init itself.</param>
    public static void LoadGameState(string stateFileName, Board board, Node parent)
    {
        var state = ResourceIO.Load<GameStateData>(Path.Combine(Global.SavePath, stateFileName)) ?? new GameStateData();

        var initializedUnits = _initializeUnits(parent, state.UnitStateResources);

        int highestId = initializedUnits.Select(unit => unit.UnitId).DefaultIfEmpty(0).Max();
        Utils.SetNextId(highestId + 1);

        board.Init(state.BoardState, initializedUnits);
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

        state.BoardState = board.State;
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
            var scenePath = $"res://scenes/units/{unitName}.tscn";
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
        }

        return initializedUnits;
    }
}