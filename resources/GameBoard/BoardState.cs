using Godot;
using Godot.Collections;

namespace AKidsDream.GameBoard;

[GlobalClass]
[Tool]
public partial class BoardState : Resource
{
	[Export(PropertyHint.Range, "1,1,1,or_greater,suffix:tiles")] public int Width;
	[Export(PropertyHint.Range, "1,1,1,or_greater,suffix:tiles")] public int Height;
	public Array<Array<TileData>> Tiles = [];
}
