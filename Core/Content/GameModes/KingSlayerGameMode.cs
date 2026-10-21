using System;
using AKidsDream.Commands;
using AKidsDream.Utilities;
using AKidsDream.Core.Managers.GameModeManager;
using AKidsDream.Managers.SaveSystems;
using Godot;

namespace AKidsDream.Core.Managers;

public partial class KingSlayerGameMode : Node, IGameMode
{
    private PlayerTeamRegistry _playerTeamRegistry;
    private int _remainingPlayers;
    
    public override void _Ready()
    {
        EventBus.Instance.UnitKilled += OnUnitKilled;
    }
    
    public override void _ExitTree()
    {
        EventBus.Instance.UnitKilled -= OnUnitKilled;
    }

    public void Init(GameContext ctx)
    {
        _playerTeamRegistry = ctx.PlayerTeamRegistry;
        _remainingPlayers = _playerTeamRegistry.PlayerCount;
    }
    
    // ----------------------------------------------- SIGNAL HANDLERS -----------------------------------------------

    private void OnUnitKilled(Unit unit)
    {
        if (unit.UnitType != Global.UnitType.King)
            return;
        
        _playerTeamRegistry.GetPlayer(unit.OwnerId).Defeated = true;
        _remainingPlayers--;
 
        // run check at the end so that when damaging unit, it can still draw, on after all abilities have ended
        if (_remainingPlayers == 1)
        {
            throw new NotImplementedException();
        }
        else if (_remainingPlayers == 0)
        {
            throw new NotImplementedException();
        }
    }
}