using AKidsDream.Core.Controllers;
using Godot;

namespace AKidsDream.Core.Teams;

[GlobalClass]
public partial class PlayerData : Resource
{
    [Export] public int PlayerIdInt;
    [Export] public string PlayerName;
    [Export] public int TeamIdInt;
    [Export] public ControllerType ControllerType;
    
    public PlayerId PlayerId => new(PlayerIdInt);
    public TeamId TeamId => new(TeamIdInt);
    public IPlayerController Controller { get; set; }
    
    public PlayerData() { }
    
    public PlayerData(PlayerId playerId, string playerName, TeamId teamId, ControllerType controllerType)
    {
        PlayerIdInt = playerId.Value;
        PlayerName = playerName;
        TeamIdInt = teamId.Value;
        ControllerType = controllerType;
    }
}