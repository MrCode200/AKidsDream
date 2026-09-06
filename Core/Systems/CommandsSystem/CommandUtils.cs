#nullable enable
using System.Threading.Tasks;
using AKidsDream.Common.Errors;
using AKidsDream.Common.Results;
using AKidsDream.Core.Managers;
using AKidsDream.Core.Teams;
using AKidsDream.GameBoard;
using AKidsDream.Managers;
using AKidsDream.Managers.SaveSystems;
using Godot;

namespace AKidsDream.Commands;

/// <summary>
/// Base interface for all commands.
/// </summary>
public interface IBaseCommand { }

/// <summary>
/// A command that can be executed in the game.
/// </summary>
public interface IGameCommand : IBaseCommand
{
    Result<GameError> Execute(GameContext context);
}

/// <summary>
/// An async command that can be executed in the game.
/// </summary>
public interface IAsyncGameBaseCommand : IBaseCommand
{
    Task<Result<GameError>> ExecuteAsync(GameContext context);
}

public sealed class GameContext(
    GameManager gameManager,
    PlayerTeamRegistry playerTeamRegistry,
    TeamRelationResolver teamRelationResolver,
    Board board,
    EventBus eventBus,
    GameLoopManager gameLoopManager,
    AbilityVisualizer abilityVisualizer,
    Node entityLayer,
    CommandExecutor commandExecutor
)
{
    public GameManager GameManager { get; } = gameManager;
    public Board Board { get; } = board;
    public EventBus EventBus { get; } = eventBus;
    public GameLoopManager GameLoopManager { get; } = gameLoopManager;
    public AbilityVisualizer AbilityVisualizer { get; } = abilityVisualizer;
    public Node EntityLayer { get; } = entityLayer;
    public CommandExecutor CommandExecutor { get; } = commandExecutor;
    public PlayerTeamRegistry PlayerTeamRegistry { get; } = playerTeamRegistry;
    public TeamRelationResolver TeamRelationResolver { get; } = teamRelationResolver;

    public SceneTree GetTree() => GameManager.GetTree();
}
