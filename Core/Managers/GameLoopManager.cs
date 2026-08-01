using Godot;
using AKidsDream.Units.Resources.Components;
using AKidsDream.Managers.SaveSystems;
using AKidsDream.Units.Resources;
using AKidsDream.Common.Logging;
using Serilog;

namespace AKidsDream.Managers;

[GlobalClass]
public partial class GameLoopManager : Node
{
    private readonly ILogger _log = GameLogger.For<GameLoopManager>();

    public int CurrentTurn;
    public bool PlayerPlayed;
    public bool EnemyPlayed;

    public override void _Ready()
    {
        EventBus.Instance.BoardGenerated += () =>
        {
            if (CurrentTurn == 0)
            {
                _log.Here().Info("Board generated, starting first turn if CurrentTurn is 0");
                StartNewTurn();
            }
        };
    }

    public void StartNewTurn()
    {
        // Update Actions 
        _log.Here().Info("Resetting ability pools"); // CHECK:
        // if should become a command,
        // will create clutter though if command logs itself
        foreach (var node in GetTree().GetNodesInGroup(nameof(Global.Groups.PlayerUnits)))
        {
            var unit = (Unit)node;
            unit.AbilityC.ResetPool();
        }

        CurrentTurn++;
        _log.Here().Info("Turn {TurnNumber} started", CurrentTurn);
    }

    public void EndPlayerTurn()
    {
        foreach (var node in GetTree().GetNodesInGroup(nameof(Global.Groups.PlayerUnits)))
        {
            var unit = (Unit)node;
        }

        PlayerPlayed = true;
        _log.Here().Info("Player turn {TurnNumber} ended", CurrentTurn);
        CheckIfToStartNewTurn();
    }

    public void EndEnemyTurn()
    {
        _log.Here().Info("Enemy turn {TurnNumber} ended", CurrentTurn);
        CheckIfToStartNewTurn();
    }

    public void CheckIfToStartNewTurn()
    {
        if (PlayerPlayed && EnemyPlayed)
        {
            StartNewTurn();
        }
    }
}