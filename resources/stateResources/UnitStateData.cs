using AKidsDream.Globals;
using AKidsDream.Units;
using Godot;

namespace AKidsDream.resources.stateResources;

[Tool]
public partial class UnitStateData : Resource
{
    // CHECK: if public or private
    [Export] public int UnitId;
    [Export] public Global.UnitName UnitName;
    [Export] public Global.UnitTeam Team;
    [Export] public Vector2I TileLocation;
    [Export] public UnitStatsData UnitStats;

    public static UnitStateData Create(Unit unit)
    {
        return new UnitStateData
        {
            UnitId = unit.UnitId,
            UnitName = unit.UnitName,
            Team = unit.Team,
            TileLocation = unit.TileLocation,
            UnitStats = unit.UnitStats
        };
    }
}