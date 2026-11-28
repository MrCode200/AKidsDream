using System;
using AKidsDream.Abilities.Effects;
using AKidsDream.Commands;
using AKidsDream.Core.Content;
using AKidsDream.Utilities;
using AKidsDream.Core.Managers.GameModeManager;
using AKidsDream.GameBoard;
using AKidsDream.Managers.SaveSystem.Resources;
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
        
        SetupPlayers(ctx.Board.State);
    }
    
    private void SetupPlayers(BoardState boardState)
    {
        var i = 0;
        foreach (var player in _playerTeamRegistry.GetAllPlayers())
        {
            var spawnPattern = new RectPattern();
            spawnPattern.FromCenter = true;
            
            switch (i)
            {
                case 0:
                    player.OwnedOriginTile = new Vector2I((boardState.Width - 1 ) / 2, boardState.Height - 1);
                    spawnPattern.Size = new Vector2I(boardState.Width, 2);
                    break;
                case 1:
                    player.OwnedOriginTile = new Vector2I((boardState.Width - 1) / 2, 1);
                    spawnPattern.Size = new Vector2I(boardState.Width, 2);
                    break;
                case 2:
                    player.OwnedOriginTile = new Vector2I(boardState.Width - 1, (boardState.Height + 1) / 2);
                    spawnPattern.Size = new Vector2I(2, boardState.Height);
                    break;
                case 3:
                    player.OwnedOriginTile = new Vector2I(0, (boardState.Height + 1) / 2);
                    spawnPattern.Size = new Vector2I(2, boardState.Height);
                    break;
                default:
                    throw new NotSupportedException("Player's count above 4 is not Support for this game Mode");
            }
            
            player.OwnedAreaPatter = spawnPattern;
            i++;
        }
    }
    
    // ----------------------------------------------- SIGNAL HANDLERS -----------------------------------------------

    private void OnUnitKilled(Unit unit)
    {
        if (unit.UnitTypeName != nameof(UnitTypeName.King))
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