using System;
using System.IO;
using AKidsDream.GameBoard;
using AKidsDream.Globals;
using AKidsDream.Units;
using Godot;
using Godot.Collections;

namespace AKidsDream.editorTools;

[Tool]
[GlobalClass]
public partial class SaveBoard : EditorScript
{
    private BoardState _save = new();
    private string _fileName;

    public override void _Run()
    {
        _promptFileName();
    }

    private void _promptFileName()
    {
        Window window = new Window();
        EditorInterface.Singleton.PopupDialog(window,
            new Rect2I(new Vector2I(100, 100), new Vector2I(400, 200))
        );

        window.SetTitle("Save Board");

        VBoxContainer container = new VBoxContainer();
        window.AddChild(container);

        TextEdit input = new();
        input.PlaceholderText = "board.tres";
        input.Text = $"DevBoardSave{DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")}.tres";
        input.CustomMinimumSize = new Vector2(350, 40);
        container.AddChild(input);

        HBoxContainer buttonContainer = new HBoxContainer();
        container.AddChild(buttonContainer);

        Button saveBtn = new Button();
        saveBtn.Text = "Save";
        saveBtn.CustomMinimumSize = new Vector2(135, 40);
        saveBtn.Connect("pressed", Callable.From(() =>
        {
            _fileName = input.Text.Trim();
            if (!_fileName.EndsWith(".tres") && !_fileName.EndsWith(".res"))
            {
                _fileName += ".tres";
            }

            _saveBoard();
        }));
        buttonContainer.AddChild(saveBtn);

        Button loadBtn = new Button();
        loadBtn.Text = "Load";
        loadBtn.CustomMinimumSize = new Vector2(135, 40);
        loadBtn.Connect("pressed", Callable.From(() =>
        {
            _fileName = input.Text.Trim();
            if (!_fileName.EndsWith(".tres") && !_fileName.EndsWith(".res"))
            {
                _fileName += ".tres";
            }

            _loadBoard();
        }));
        buttonContainer.AddChild(loadBtn);

        window.CloseRequested += window.QueueFree;
    }

    private void _saveBoard()
    {
        var sceneRoot = EditorInterface.Singleton.GetEditedSceneRoot();
        Board board = sceneRoot as Board;

        // If root is not a Board, search for it in the scene tree
        if (board == null && sceneRoot != null)
        {
            board = sceneRoot.FindChild("Board", recursive: true) as Board;
        }

        if (board == null)
        {
            GD.PrintErr("No Board found in the edited scene. Please run this script on a Board scene.");
            return;
        }


        // Set board dimensions
        _save.Width = board.State.Width;
        _save.Height = board.State.Height;

        // Copy tile data from current board state
        _save.Tiles = board.State.Tiles;

        // Get all units from the scene
        _save.InitialUnits.Clear();

        Array<Node> units = sceneRoot.FindChildren("*", "Unit");
        GD.Print($"Found {units.Count} units");

        foreach (var node in units)
        {
            CharacterBody2D unit = node as CharacterBody2D;

            Vector2I tilePosition = new Vector2I(
                (int)(unit.Position.X / Global.TileSize),
                (int)(unit.Position.Y / Global.TileSize)
            );

            Resource rawStats = node.Get("Stats").As<Resource>();
            if (rawStats == null)
            {
                GD.PrintErr($"No Stats resource at all for unit at {tilePosition}");
                continue;
            }

            StatsData stats = Utils.RebuildTyped<StatsData>(rawStats);
            _save.InitialUnits[tilePosition] = stats;
        }

        ResourceIO.Save(_save, Path.Combine(Global.SavePath, _fileName));
        GD.Print($"Board saved to {_fileName}");
    }

    private void _loadBoard()
    {
        var sceneRoot = EditorInterface.Singleton.GetEditedSceneRoot();
        Board board = sceneRoot as Board;

        // If root is not a Board, search for it in the scene tree
        if (board == null && sceneRoot != null)
        {
            board = sceneRoot.FindChild("Board", recursive: true) as Board;
        }

        if (board == null)
        {
            GD.PrintErr("No Board found in the edited scene. Please run this script on a Board scene.");
            return;
        }

        string filePath = Path.Combine(Global.SavePath, _fileName);
        if (!File.Exists(filePath))
        {
            GD.PrintErr($"Save file not found: {filePath}");
            return;
        }

        BoardState loadedState = ResourceIO.Load<BoardState>(filePath);
        if (loadedState == null)
        {
            GD.PrintErr($"Failed to load board state from {filePath}");
            return;
        }

        // Remove existing units
        Array<Node> existingUnits = sceneRoot.FindChildren("*", "Unit");
        GD.Print($"Removing {existingUnits.Count} existing units");
        foreach (var unit in existingUnits)
        {
            unit.QueueFree();
        }

        // Create and place units from saved data
        Global.UnitName loadedUnitName = Global.UnitName.Soldier;
        PackedScene unitScene = ResourceLoader.Load<PackedScene>(
            Path.Combine(Global.UnitScenePath, loadedUnitName.GetFieldValue<string>() + ".tscn")
        );

        Dictionary rawDict = (Dictionary)loadedState.InitialUnits; // implicit downcast to untyped

        foreach (Vector2I tileLocation in loadedState.InitialUnits.Keys)
        {
            Resource rawStats = rawDict[tileLocation].As<Resource>();
            StatsData stats = Utils.RebuildTyped<StatsData>(rawStats);
            if (loadedUnitName != stats.UnitName)
            {
                loadedUnitName = stats.UnitName;
                unitScene = ResourceLoader.Load<PackedScene>(
                    Path.Combine(Global.UnitScenePath, loadedUnitName.GetFieldValue<string>() + ".tscn")
                );
                if (unitScene == null)
                {
                    GD.PrintErr("Failed to load unit scene template");
                    return;
                }
            }

            CharacterBody2D unit = unitScene.Instantiate<CharacterBody2D>();
            unit.Set("Stats", stats);
            unit.Set("TileLocation", tileLocation);
            unit.Position = Board.TileToWorldPosition(tileLocation);

            sceneRoot.GetNode<Node>("EntityLayer").AddChild(unit);
            unit.Owner = sceneRoot;
            
            GD.Print($"Loaded unit at {tileLocation} || {unit.Position}");
        }

        EditorInterface.Singleton.MarkSceneAsUnsaved();
        GD.Print($"Board loaded from {_fileName}");
    }
}