using System.IO;
using System.Linq;
using AKidsDream.GameBoard;
using AKidsDream.Managers.SaveSystems;
using AKidsDream.Managers.SaveSystem.Resources;
using AKidsDream.Units.Resources;
using Godot;

namespace AKidsDream.editorTools;

[Tool]
[GlobalClass]
public partial class SaveLoadGameETool : EditorScript
{
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
        input.PlaceholderText = "GameState.tres";
        input.Text = $"DevBoardSave.tres";
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

            _saveGame();
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

    private void _saveGame()
    {
        var sceneRoot = EditorInterface.Singleton.GetEditedSceneRoot();
        Board board = sceneRoot as Board;

        if (board == null && sceneRoot != null)
        {
            board = sceneRoot.FindChild("Board", recursive: true) as Board;
        }

        if (board == null)
        {
            GD.PrintErr("No Board found in the edited scene. Please run this script on a Board scene.");
            return;
        }
        
        // SaveLoadManager.SaveState(board, _fileName);
        GameStateData _save = new GameStateData();
        _save.BoardStateData = board.StateData;        
        
        Unit[] units = sceneRoot.FindChildren("*", "Unit").Cast<Unit>().ToArray();
        
        foreach (var unit in units)
        {
            unit.TileLocation = Board.WorldPositionToTilePosition(unit.Position);
            _save.UnitStateResources.Add(UnitStateData.Create(unit));
            GD.Print($"Unit {unit.UnitName} at {unit.TileLocation} saved.");
        }
        
        ResourceIO.Save(_save, Path.Combine(Global.SavePath, _fileName));
        GD.Print($"Board saved to {_fileName}");
    }

    private void _loadBoard()
    {
        var sceneRoot = EditorInterface.Singleton.GetEditedSceneRoot();
        Board board = sceneRoot as Board;

        if (board == null && sceneRoot != null)
        {
            board = sceneRoot.FindChild("Board", recursive: true) as Board;
        }

        if (board == null)
        {
            GD.PrintErr("No Board found in the edited scene. Please run this script on a Board scene.");
            return;
        }

        // Create and place units from saved UnitStateData
        Node entityLayer = sceneRoot.FindChild("EntityLayer", recursive: true);
        if (entityLayer == null)
        {
            GD.PrintErr("EntityLayer not found in scene");
            return;
        }
        SaveLoadManager.LoadGameState(_fileName, board, entityLayer);

        var units = board.GetAllUnits();
        if (units is null) return;
        foreach (var unit in units)
            unit.Owner = sceneRoot;
    }
}