#nullable enable
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using AKidsDream.Common.Errors;
using AKidsDream.Common.Logging;
using Godot;
using Serilog;

namespace AKidsDream.Commands;

[GlobalClass]
public partial class CommandExecutor : Node
{
    private GameContext _context = null!;
    private readonly ILogger _log = GameLogger.For<CommandExecutor>();

    public void Init(GameContext context)
    {
        _context = context;
    }

    public CommandResult Execute(IGameCommand command)
    {
        var commandName = command.GetType().Name;
        _log.Here().Debug("Executing command {CommandType}", commandName);

        var sw = Stopwatch.StartNew();
        CommandResult result;
        try
        {
            result = command.Execute(_context);
            sw.Stop();

            if (result.IsFailure)
            {
                _log.Here().Warn(
                    "Command {CommandName} failed with code {ErrorCode} ({ElapsedMs}ms): {FailureReason}",
                    commandName,
                    result.Error?.Code ?? "UNKNOWN",
                    sw.ElapsedMilliseconds,
                    result.FailureReason);
                
                _log.ForContext("CommandType", commandName)
                    .ForContext("ErrorCode", result.Error?.Code ?? "UNKNOWN")
                    .ForContext("ErrorMessage", result.FailureReason)
                    .ForContext("DurationMs", sw.ElapsedMilliseconds)
                    .ForContext("ErrorType", result.Error?.GetType().Name ?? "Unknown")
                    .Here().Debug("Command failure details");
            }
            else
            {
                _log.Here().Debug(
                    "Command {CommandType} succeeded ({ElapsedMs}ms)",
                    commandName,
                    sw.ElapsedMilliseconds);
            }
        }
        catch (Exception e)
        {
            sw.Stop();
            _log.Here().Err(e, "Command {CommandType} failed with unhandled exception ({ElapsedMs}ms)", commandName, sw.ElapsedMilliseconds);
            _log.ForContext("CommandType", commandName)
                .ForContext("ExceptionType", e.GetType().Name)
                .ForContext("ExceptionMessage", e.Message)
                .ForContext("DurationMs", sw.ElapsedMilliseconds)
                .Here().Debug("Command exception details");
            result = CommandResult.Fail(command, new CommandError.ExceptionOccurred(e));
        }

        return result;
    }

    public async Task<CommandResult> ExecuteAsync(IAsyncGameBaseCommand baseCommand)
    {
        var commandName = baseCommand.GetType().Name;
        _log.Here().Debug("Executing async command {CommandType}", commandName);

        var sw = Stopwatch.StartNew();
        CommandResult result;
        try
        {
            result = await baseCommand.ExecuteAsync(_context);
            sw.Stop();

            if (result.IsFailure)
            {
                _log.Here().Warn(
                    "Async command {CommandName} failed with code {ErrorCode} ({ElapsedMs}ms): {FailureReason}",
                    commandName,
                    result.Error?.Code ?? "UNKNOWN",
                    sw.ElapsedMilliseconds,
                    result.FailureReason);
                
                _log.ForContext("CommandType", commandName)
                    .ForContext("ErrorCode", result.Error?.Code ?? "UNKNOWN")
                    .ForContext("ErrorMessage", result.FailureReason)
                    .ForContext("DurationMs", sw.ElapsedMilliseconds)
                    .ForContext("ErrorType", result.Error?.GetType().Name ?? "Unknown")
                    .Here().Debug("Async command failure details");
            }
            else
            {
                _log.Here().Debug(
                    "Async command {CommandType} succeeded ({ElapsedMs}ms)",
                    commandName,
                    sw.ElapsedMilliseconds);
            }
        }
        catch (Exception e)
        {
            sw.Stop();
            _log.Here().Err(e, "Async command {CommandType} failed with unhandled exception ({ElapsedMs}ms)", commandName, sw.ElapsedMilliseconds);
            _log.ForContext("CommandType", commandName)
                .ForContext("ExceptionType", e.GetType().Name)
                .ForContext("ExceptionMessage", e.Message)
                .ForContext("DurationMs", sw.ElapsedMilliseconds)
                .Here().Debug("Async command exception details");
            result = CommandResult.Fail(baseCommand, new CommandError.ExceptionOccurred(e));
        }

        return result;
    }
}
