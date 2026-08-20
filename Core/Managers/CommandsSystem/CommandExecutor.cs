using Godot;
using System;
using System.Threading.Tasks;
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

        CommandResult result;
        try
        {
            result = command.Execute(_context);

            if (!result.Success)
            {
                _log.Here().Warn(
                    "Command {CommandName} failed with due to {FailureType} with reason: {FailureReason}",
                    command.GetType().Name,
                    result.FailureType,
                    result.FailureReason);
            }
            else
            {
                _log.Here().Debug(
                    "Command {CommandType} succeeded",
                    command.GetType().Name);
            }
        }
        catch (Exception e)
        {
            _log.Here().Error(e, "Command {CommandType} failed with exception", command.GetType().Name);
            result = CommandResult.Fail(command, CommandFailureType.Unknown, e.Message);
        }

        return result;
    }

    public async Task<CommandResult> ExecuteAsync(IAsyncGameBaseCommand baseCommand)
    {
        _log.Here().Debug(
            "Executing async command {CommandType}",
            baseCommand.GetType().Name
        );

        CommandResult result;
        try
        {
            result = await baseCommand.ExecuteAsync(_context);

            if (!result.Success)
            {
                _log.Here().Warn(
                    "Async command {CommandName} failed with due to {FailureType} with reason: {FailureReason}",
                    baseCommand.GetType().Name,
                    result.FailureType,
                    result.FailureReason);
            }
            else
            {
                _log.Here().Debug(
                    "Async command {CommandType} succeeded",
                    baseCommand.GetType().Name);
            }
        }
        catch (Exception e)
        {
            _log.Here().Error(e, "Async command {CommandType} failed with exception", baseCommand.GetType().Name);
            result = CommandResult.Fail(baseCommand, CommandFailureType.Unknown, e.Message);
        }

        return result;
    }
}