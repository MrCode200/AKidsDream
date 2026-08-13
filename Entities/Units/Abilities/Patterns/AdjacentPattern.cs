using System.Linq;
using AKidsDream.GameBoard;
using Godot;

namespace AKidsDream.Abilities.Effects;

[GlobalClass]
[Tool]
public partial class AdjacentPattern : AccessFieldPattern
{
	[Export] public int Radius = 1;
	[Export] public int Width = 1;
	public override Vector2I[] GetTilesUnfiltered(Vector2I origin, Board board)
	{
		var numRowTiles = 2 * Radius + 1;
		
		// 9 (3x3), 25 (5x5), 49 (7x7) 
		// i %: 3, 5, 7 // -1, -2, -3
		
		return Enumerable.Range(0, numRowTiles * numRowTiles)
			.Where(i => i <= numRowTiles * Width || numRowTiles * (numRowTiles - Width) < i || // Let Through Top and Bottom Rows
			            i % numRowTiles <= (Width - 1) || i % numRowTiles >= numRowTiles - Width) // Let Through Left and Right Columns size of Width
			.Select(i => new Vector2I(i % numRowTiles - Radius, i / numRowTiles - Radius)) 
			.Select(offset => origin + offset)
			.Where(board.TileInBoard)
			.ToArray();
	}
}
