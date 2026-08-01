using AKidsDream.Common.Logging;
using AKidsDream.GameBoard;
using AKidsDream.Managers.SaveSystems;
using Godot;
using Serilog;

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
    
    private readonly ILogger _log = GameLogger.For<GameManager>();
    
    public override void _Ready()
    {
        GameLogger.Setup();
        _log.Here().Info(
            "GameManager initializing with LoadFileName: '{LoadFileName}', SaveFileName: '{SaveFileName}'",
            LoadFileName,
            SaveFileName);
        SaveLoadManager.LoadGameState(LoadFileName, GameBoard, EntityLayer);
    }

    public override void _ExitTree()
    {
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
    }
}