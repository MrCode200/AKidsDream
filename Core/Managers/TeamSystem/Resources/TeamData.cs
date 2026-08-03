using Godot;

namespace AKidsDream.Core.Teams;

[GlobalClass]
public partial class TeamData : Resource
{
    [Export] public int TeamIdInt;
    // [Export] public string DisplayName;
    
    public TeamId TeamId => new(TeamIdInt);
    
    public TeamData() { }
    
    public TeamData(TeamId teamId)
    {
        TeamIdInt = teamId.Value;
        // DisplayName = displayName;
    }
}