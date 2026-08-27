using AKidsDream.Core.Controllers;
using AKidsDream.Managers.SaveSystems;
using Godot;

namespace AKidsDream.Core.Teams;

[GlobalClass]
public partial class PlayerData : Resource
{
    [Export] public int PlayerIdInt;
    [Export] public string PlayerName;
    [Export] public int TeamIdInt;
    [Export] public Global.UnitColor UnitColor;
    [Export] public ControllerType ControllerType;

    private int _mana;
    [Export]
    public int Mana
    {
        get { return _mana; }
        set
        {
            _mana = Mathf.Max(value, 0);
        }
    }
    
    public PlayerId PlayerId => new(PlayerIdInt);
    public TeamId TeamId => new(TeamIdInt);
    public IPlayerController Controller { get; set; }
    
    public PlayerData() { }
    
    public PlayerData(PlayerId playerId, string playerName, TeamId teamId, Global.UnitColor unitColor, ControllerType controllerType)
    {
        PlayerIdInt = playerId.Value;
        PlayerName = playerName;
        TeamIdInt = teamId.Value;
        UnitColor = unitColor;
        ControllerType = controllerType;
    }
}