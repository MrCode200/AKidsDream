using System;
using AKidsDream.GameBoard;
using AKidsDream.Globals;
using Godot;

namespace AKidsDream.editorTools;

[Tool]
[GlobalClass]
public partial class SaveLoadBoardETool : EditorScript
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

        if (board == null && sceneRoot != null)
        {
            board = sceneRoot.FindChild("Board", recursive: true) as Board;
        }

        if (board == null)
        {
            GD.PrintErr("No Board found in the edited scene. Please run this script on a Board scene.");
            return;
        }
        
        SaveLoadManager.SaveState(board, _fileName);
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
    }
}