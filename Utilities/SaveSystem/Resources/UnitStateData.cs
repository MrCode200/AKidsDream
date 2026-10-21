using AKidsDream.Managers.SaveSystems;
using Godot;

namespace AKidsDream.Utilities;

// TODO: add pool count etc to save logic
[Tool]
public partial class UnitStateData : Resource
{
    [Export] public int UnitId;
    [Export] public int OwnerId;
    
    [Export] public string UnitKey;
    [Export] public Vector2I TileLocation;
    [Export] public UnitStatsData UnitStats;

    public static UnitStateData Create(Unit unit)
    {
        return new UnitStateData
        {
            UnitId = unit.UnitId.Value,
            OwnerId = unit.OwnerIdInt,
            UnitKey = unit.ToString(),
            TileLocation = unit.TileLocation,
            UnitStats = (UnitStatsData)unit.UnitStats.Duplicate()
        };
    }
}