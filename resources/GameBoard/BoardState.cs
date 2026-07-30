using Godot;
using Godot.Collections;
using TileData = AKidsDream.GameBoard.TileData;

namespace AKidsDream.resources.stateResources;

[GlobalClass]
[Tool]
public partial class BoardState : Resource
{
	[Export(PropertyHint.Range, "1,1,1,or_greater,suffix:tiles")] public int Width = 9;
	[Export(PropertyHint.Range, "1,1,1,or_greater,suffix:tiles")] public int Height = 9;
	public Array<Array<TileData>> Tiles = [];
}
