using AKidsDream.Common;
using AKidsDream.Common.Logging;
using Godot;

namespace AKidsDream.Managers.SaveSystem.Resources;

[GlobalClass]
public partial class TileData : Resource
{
    public readonly Vector2I TileLocation;
    public Unit Unit;

    public TileData()
    {
    }

    public TileData(Vector2I tileLocation, Unit unit = null)
    {
        if (tileLocation.X < 0 || tileLocation.Y < 0)
            GameLogger.For<TileData>().Here().Warn(
                "TileData Created With (X/Y) < 0: {TileLocation}" +
                "This may cause logic issues, as negative Vector2I represent null",
                tileLocation
            );

        TileLocation = tileLocation;
        Unit = unit;
    }
}