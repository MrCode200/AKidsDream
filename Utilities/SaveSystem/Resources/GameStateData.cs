using System.Linq;
using AKidsDream.Core.Teams;
using AKidsDream.Core.Controllers;
using AKidsDream.Utilities.TypeExtensions;
using AKidsDream.Units.Resources;
using Godot;
using Godot.Collections;

namespace AKidsDream.Managers.SaveSystem.Resources;

[Tool]
public partial class GameStateData : Resource
{
    [Export] public int GameRound;  
    [Export] private Dictionary<int, PlayerData> _playerTurnOrder;
    [Export] public int ActivePlayerIdInt;
    [Export] public BoardStateData BoardStateData = new();
    [Export] public Array<PlayerData> PlayerData = [];
    [Export] public int LocalPlayerIdInt;
    [Export] public Array<TeamData> TeamData = [];
    [Export] private Dictionary<Vector2I, TeamRelation> _teamRelations = new();
    [Export] public Array<UnitStateData> UnitStateResources = [];

    public System.Collections.Generic.Dictionary<PlayerId, PlayerData> PlayerTurnOrder
    {
        get
        {
            return _playerTurnOrder.ToDictionary(
                x => new PlayerId(x.Key),
                x => x.Value
            );
        }
        set
        {
            _playerTurnOrder = value.ToDictionary(
                x => x.Key.Value,
                x => x.Value
            ).ToGodotDictionary();
        }
    }
    
    public System.Collections.Generic.Dictionary<(TeamId, TeamId), TeamRelation> TeamRelations
    {
        get
        {
            return _teamRelations.ToDictionary(
                x => (new TeamId(x.Key.X), new TeamId(x.Key.Y)),
                x => x.Value
            );
        }
        set
        {
            _teamRelations = value.ToDictionary(
                x => new Vector2I(x.Key.Item1.Value, x.Key.Item2.Value),
                x => x.Value
            ).ToGodotDictionary();
        }
    }
}