using AKidsDream.Managers.SaveSystems;
using Godot;

namespace AKidsDream.Units.Resources;

[Tool]
public partial class UnitStateData : Resource
{
    [Export] public int UnitId;
    [Export] public int OwnerId;
    
    [Export] public Global.UnitName UnitName;
    [Export] public Vector2I TileLocation;
    [Export] public UnitStatsData UnitStats;

    public static UnitStateData Create(Unit unit)
    {
        return new UnitStateData
        {
            UnitId = unit.UnitId.Value,
            OwnerId = unit.OwnerIdInt,
            UnitName = unit.UnitName,
            TileLocation = unit.TileLocation,
            UnitStats = (UnitStatsData)unit.UnitStats.Duplicate()
        };
    }
}