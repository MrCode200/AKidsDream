using AKidsDream.GameBoard;
using Godot;

namespace AKidsDream.Abilities.Effects;

[GlobalClass]
[Tool]
public partial class OneTilePattern : AccessFieldPattern
{
	// [RequiresOrigin]
	public override Vector2I[] GetTilesUnfiltered(Vector2I? origin, Board board)
	{
		if (origin == null) return [];
		return board.TileInBoard(origin.Value) ? [origin.Value] : [];
	}
}
