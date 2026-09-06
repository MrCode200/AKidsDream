#nullable enable
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using AKidsDream.Common.Errors;
using AKidsDream.Common.Logging;
using AKidsDream.Common.Results;
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

    public Result<GameError> Execute(IGameCommand command)
    {
        var commandName = command.GetType().Name;
        _log.Here().Debug("Executing command {CommandType}", commandName);

        var sw = Stopwatch.StartNew();
        Result<GameError> result;
        try
        {
            result = command.Execute(_context);
            sw.Stop();
            LogCommandResult(commandName, result, sw.ElapsedMilliseconds, isAsync: false);
        }
        catch (Exception e)
        {
            sw.Stop();
            LogCommandException(commandName, e, sw.ElapsedMilliseconds, isAsync: false);
            result = Result.Fail<GameError>(new UnexpectedError(e));
        }

        return result;
    }

    public async Task<Result<GameError>> ExecuteAsync(IAsyncGameBaseCommand baseCommand)
    {
        var commandName = baseCommand.GetType().Name;
        _log.Here().Debug("Executing async command {CommandType}", commandName);

        var sw = Stopwatch.StartNew();
        Result<GameError> result;
        try
        {
            result = await baseCommand.ExecuteAsync(_context);
            sw.Stop();
            LogCommandResult(commandName, result, sw.ElapsedMilliseconds, isAsync: true);
        }
        catch (Exception e)
        {
            sw.Stop();
            LogCommandException(commandName, e, sw.ElapsedMilliseconds, isAsync: true);
            result = Result.Fail<GameError>(new UnexpectedError(e));
        }

        return result;
    }
    
    private void LogCommandResult(string commandName, Result<GameError> result, long durationMs, bool isAsync = false)
    {
        var prefix = isAsync ? "Async command" : "Command";

        if (result.IsFailure)
        {
            _log.ForContext("CommandType", commandName)
                .ForContext("ErrorCode", result.Error?.Code ?? "UNKNOWN")
                .ForContext("ErrorMessage", result.Error?.Message)
                .ForContext("DurationMs", durationMs)
                .ForContext("ErrorType", result.Error?.GetType().Name ?? "Unknown")
                .Here().Err("{Prefix} failed {ErrorCode}: {ErrorMessage}", prefix);
        }
        else
        {
            _log.Here().Debug(
                "{Prefix} {CommandType} succeeded ({ElapsedMs}ms)",
                prefix,
                commandName,
                durationMs);
        }
    }

    private void LogCommandException(string commandName, Exception e, long durationMs, bool isAsync = false)
    {
        var prefix = isAsync ? "Async command" : "Command";
        _log.ForContext("CommandType", commandName)
            .ForContext("ExceptionType", e.GetType().Name)
            .ForContext("ExceptionMessage", e.Message)
            .ForContext("DurationMs", durationMs)
            .Here().Err(e, "{Prefix} {CommandType} exception {ExceptionType} details: {ExceptionMessage}", prefix, commandName, e.GetType().Name, e.Message);
    }
}
