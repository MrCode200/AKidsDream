using Godot;
using Godot.Collections;

namespace AKidsDream.Managers.SaveSystem.Resources;

[GlobalClass]
[Tool]
public partial class BoardStateData : Resource
{
	[Export(PropertyHint.Range, "1,1,1,or_greater,suffix:tiles")] public int Width = 9;
	[Export(PropertyHint.Range, "1,1,1,or_greater,suffix:tiles")] public int Height = 9;
	public Array<Array<TileData>> Tiles = [];
}
