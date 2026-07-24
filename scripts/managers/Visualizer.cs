using Godot;
using AKidsDream.Units;

namespace AKidsDream.Globals;

[GlobalClass]
public partial class Visualizer : Node
{
	[Export] public TileMapLayer Tilemap;
	
	public void ShowUnitValidMoves(Unit unit)
	{
		var validMoves = unit.MoveC.ValidMoves();
		GD.Print($"MoveTile: {string.Join(", ", validMoves)}");

		foreach (var tile in validMoves)
		{
			Tilemap.SetCell(
				tile,
				0,
				new Vector2I(4, 12)
			);
		}

		var validAttacks = unit.AttackC.ValidAttacks();
		GD.Print($"AttackTile: {string.Join(", ", validAttacks)}");

		foreach (var tile in validAttacks)
		{
			Tilemap.SetCell(
				tile,
				0,
				new Vector2I(5, 12)
			);
		}
	}
}
