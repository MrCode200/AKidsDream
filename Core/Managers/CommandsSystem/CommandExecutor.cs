using Godot;
using System;
using AKidsDream.GameBoard;
using AKidsDream.Managers.SaveSystems;
using AKidsDream.Managers;
using AKidsDream.Common.Logging;
using Serilog;

namespace AKidsDream.Commands;

[GlobalClass]
public partial class CommandExecutor : Node
{
    [Export] public AbilityVisualizer AbilityVisualizer;
    [Export] public Board Board;
    [Export] public GameLoopManager GameLoopManager;
    private GameContext _context;
    private readonly ILogger _log = GameLogger.For<CommandExecutor>();

    public override void _Ready()
    {
        _context = new GameContext(
            Board,
            EventBus.Instance,
            GameLoopManager,
            AbilityVisualizer
        );
        _log.Here().Info("CommandExecutor initialized with GameContext");
    }

    public CommandResult Execute(IGameCommand command)
    {
        _log.Here().Debug(
            "Executing command {CommandType}",
            command.GetType().Name
            );
        
        var result = command.Execute(_context);
        
        if (!result.Success)
        {
            _log.Here().Warn(
                "Command {CommandType} failed with reason: {FailureReason}",
                command.GetType().Name,
                result.FailureReason);
        }
        else
        {
            _log.Here().Debug(
                "Command {CommandType} succeeded",
                command.GetType().Name);
        }
        
        return result;
    }
}