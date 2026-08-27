using System.Threading.Tasks;
using AKidsDream.GameBoard;
using AKidsDream.Managers.SaveSystems;
using AKidsDream.Managers;
using AKidsDream.Common.Components;

namespace AKidsDream.Commands;

/// <summary>
/// Base interface for all commands.
/// </summary>
public interface IBaseCommand { }

/// <summary>
/// A command that can be executed in the game.
/// Should only log on success change...
/// </summary>
public interface IGameCommand : IBaseCommand
{
    CommandResult Execute(GameContext context);
}

/// <summary>
/// An async command that can be executed in the game.
/// Should only log on success change...
/// </summary>
public interface IAsyncGameBaseCommand : IBaseCommand
{
    Task<CommandResult> ExecuteAsync(GameContext context);
}

public enum CommandFailureType
{
    None,
    Unknown,
    
    // Argument validation (consolidated)
    NullArgument,
    InvalidArgument,
    
    // Ability-specific failures (specific - different handling)
    MaxDuplicateTargetsExceeded,
    AbilityNotFound,
    MissingAbilityPoints,
    EffectExecutionFailed,
    InvalidTargetsSelected,
    
    // Turn management failures (specific - different handling)
    NotPlayerTurn,
    
    // General failures
    InvalidTargetLocation,
    InsufficientResources,
}

public sealed class CommandResult
{
    public IBaseCommand BaseCommand;
    public bool Success { get; init; }
    public CommandFailureType FailureType { get; init; } = CommandFailureType.None;
    public string FailureReason { get; init; }

    public static CommandResult Ok(IBaseCommand baseCommand) => new() { BaseCommand = baseCommand, Success = true };

    public static CommandResult Fail(IBaseCommand baseCommand, CommandFailureType type, string reason) =>
        new() { BaseCommand = baseCommand, Success = false, FailureType = type, FailureReason = reason };
}

public sealed class GameContext(
    Board board,
    EventBus eventBus,
    GameLoopManager gameLoopManager,
    AbilityVisualizer abilityVisualizer
)
{
    public Board Board { get; } = board;
    public EventBus EventBus { get; } = eventBus;
    public GameLoopManager GameLoopManager { get; } = gameLoopManager;
    public AbilityVisualizer AbilityVisualizer { get; } = abilityVisualizer;

    public override string ToString()
    {
        return $"Board: {Board}, EventBus: {EventBus}, GameLoopManager: {GameLoopManager}, AbilityVisualizer: {AbilityVisualizer}";
    }
}

public static class CommandFailureMapper
{
    public static CommandFailureType MapCastFailureToCommandFailure(CastFailureReason reason)
    {
        return reason switch
        {
            CastFailureReason.AbilityNotFound => CommandFailureType.AbilityNotFound,
            CastFailureReason.InvalidTargetsSelected => CommandFailureType.InvalidTargetsSelected,
            CastFailureReason.TilesOutOfRange => CommandFailureType.InvalidTargetLocation,
            CastFailureReason.CannotAfford => CommandFailureType.MissingAbilityPoints,
            _ => CommandFailureType.Unknown
        };
    }
}