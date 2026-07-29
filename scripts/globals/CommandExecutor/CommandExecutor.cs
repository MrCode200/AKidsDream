using Godot;
using System;
using AKidsDream.GameBoard;
using AKidsDream.Globals;
using AKidsDream.Managers;

namespace AKidsDream.Commands;

public partial class CommandExecutor : Node
{
    [Export] public Visualizer Visualizer;
    private GameContext _context;

    public override void _Ready()
    {
        // CHECK: Is really needed? As it's a singleton?'
        _context = new GameContext(
            Board.Instance,
            EventBus.Instance,
            Visualizer
        );
    }

    public CommandResult Execute(IGameCommand command)
    {
        return command.Execute(_context);
    }
}