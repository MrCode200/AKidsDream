using System;
using AKidsDream.Commands;
using AKidsDream.Utilities.Logging;
using AKidsDream.Managers.SaveSystems;
using Godot;
using Serilog;

namespace AKidsDream.Core.Managers.GameModeManager;

public interface IGameMode
{
    public void Init(GameContext ctx);
}

public partial class GameModeLoader : Node
{
    private string _currentGameMode;
    
    private readonly ILogger _logger = GameLogger.For(typeof(GameModeLoader));
    
    public string GetCurrentGameMode() => _currentGameMode;
    public void Init(string mode, GameContext ctx)
    {
        var gameModeDef = Global.GameModeRegistry.Get(mode);
        var gameMode = gameModeDef.Factory(ctx);
        gameMode.Init(ctx);

        if (gameMode is not Node node) 
            throw new Exception($"Game mode {mode} is not of type node");
        
        AddChild(node);
        
        _currentGameMode = mode;
    }
}