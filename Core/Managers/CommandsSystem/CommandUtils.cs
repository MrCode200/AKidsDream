using AKidsDream.GameBoard;
using AKidsDream.Managers.SaveSystems;
using AKidsDream.Managers;
using Godot.Collections;

namespace AKidsDream.Commands;

public interface IGameCommand
{
    CommandResult Execute(GameContext context);
}

public sealed class CommandResult
{
    public IGameCommand Command;
    public bool Success { get; init; }
    public string FailureReason { get; init; }
    
    public static CommandResult Ok(IGameCommand command) => new() { Command = command, Success = true };
    public static CommandResult Fail(IGameCommand command, string reason) => new() { Command = command, Success = false, FailureReason = reason };
}

public sealed class GameContext(Board board, EventBus eventBus, AbilityVisualizer abilityVisualizer)
{
    public Board Board { get; init; } = board;
    public EventBus EventBus { get; init; } = eventBus;
    public AbilityVisualizer AbilityVisualizer { get; init; } = abilityVisualizer;
}