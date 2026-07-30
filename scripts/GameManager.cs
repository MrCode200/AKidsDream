using AKidsDream.GameBoard;
using AKidsDream.Globals;
using Godot;

namespace AKidsDream.scripts;

public partial class GameManager : Node2D
{
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
        SaveLoadManager.LoadGameState(LoadFileName, GameBoard, EntityLayer);
    }

    public override void _ExitTree()
    {
        if (SaveOnExit)
            SaveLoadManager.SaveState(GameBoard, SaveFileName);
    }
}