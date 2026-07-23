using AKidsDream.Units;
using Godot;

namespace AKidsDream.GameBoard;

[GlobalClass]
public partial class TileData : Resource
{
	public readonly Vector2I TileLocation;
	public Unit Unit;
	
	public TileData() { }
	
	public TileData(Vector2I tileLocation, Unit unit = null)
	{
		TileLocation = tileLocation;
		Unit = unit;
	}
	

}
