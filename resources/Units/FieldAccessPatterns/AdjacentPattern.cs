using System;
using System.Linq;
using AKidsDream.GameBoard;
using AKidsDream.Globals;
using Godot;

namespace AKidsDream.Units.FieldAccessPatterns;

[GlobalClass]
public partial class AdjacentPattern : AccessFieldPattern
{
	public override Vector2I[] GetTilesUnfiltered(Vector2I origin, Board board)
	{
		return Enumerable.Range(0, 9)
			.Select(i => new Vector2I(i % 3 - 1, i / 3 - 1))
			.Where(tile => tile != Vector2I.Zero)
			.Select(offset => origin + offset)
			.Where(move => Board.Instance.TileInBoard(move))
			.ToArray();
	}
}
