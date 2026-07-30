using Godot;
using System;
using AKidsDream.GameBoard;
using AKidsDream.Globals;
using AKidsDream.Managers;

namespace AKidsDream.Commands;

[GlobalClass]
public partial class CommandExecutor : Node
{
    [Export] public AbilityVisualizer AbilityVisualizer;
    [Export] public Board Board;
    private GameContext _context;

    public override void _Ready()
    {
        _context = new GameContext(
            Board,
            EventBus.Instance,
            AbilityVisualizer
        );
    }

    public CommandResult Execute(IGameCommand command)
    {
        return command.Execute(_context);
    }
}