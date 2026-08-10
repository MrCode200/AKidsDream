using AKidsDream.GameBoard;
using AKidsDream.Managers.SaveSystems;
using AKidsDream.Managers;

namespace AKidsDream.Commands;

/// <summary>
/// A command that can be executed in the game.
/// Should only log on success change...
/// </summary>
public interface IGameCommand
{
    CommandResult Execute(GameContext context);
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
    
    // Turn management failures (specific - different handling)
    NotPlayerTurn,
    
    // General failures
    InvalidTargetLocation,
    InsufficientResources,
}

public sealed class CommandResult
{
    public IGameCommand Command;
    public bool Success { get; init; }
    public CommandFailureType FailureType { get; init; } = CommandFailureType.None;
    public string FailureReason { get; init; }

    public static CommandResult Ok(IGameCommand command) => new() { Command = command, Success = true };

    public static CommandResult Fail(IGameCommand command, CommandFailureType type, string reason) =>
        new() { Command = command, Success = false, FailureType = type, FailureReason = reason };
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