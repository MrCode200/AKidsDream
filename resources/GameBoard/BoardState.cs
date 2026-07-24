using AKidsDream.Units;
using Godot;
using Godot.Collections;

namespace AKidsDream.GameBoard;

[GlobalClass]
[Tool]
public partial class BoardState : Resource
{
	[Export(PropertyHint.Range, "1,1,1,or_greater,suffix:tiles")] public int Width;
	[Export(PropertyHint.Range, "1,1,1,or_greater,suffix:tiles")] public int Height;
	[Export] public Dictionary<Vector2I, StatsData> InitialUnits = new();
	public Array<Array<TileData>> Tiles = [];
}
