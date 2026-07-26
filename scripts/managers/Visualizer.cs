using AKidsDream.Abilities;
using AKidsDream.GameBoard;
using Godot;
using AKidsDream.Units;

namespace AKidsDream.Globals;

[GlobalClass]
public partial class Visualizer : Node
{
	[Export] public TileMapLayer Tilemap;
	
	public void ShowReachVisualization(Unit source, Vector2I sourceTile, AbilityData ability)
	{
		var visualizationData = ability.GetReachVisualizationData(source, Board.Instance, sourceTile);
		ShowVisualization([visualizationData]);
	}
	
	public void ShowEffectVisualization(Unit source, Vector2I[] targetTiles, EffectData effect)
	{
		var visualizationData = effect.GetEffectVisualizationData(source, Board.Instance, targetTiles);
		ShowVisualization([visualizationData]);
	}
	
	public void ShowVisualization((Vector2I atlasCoord, Vector2I[] tiles)[] layers)
	{
		ClearVisualization();
		
		foreach (var (atlasCoord, tiles) in layers)
		{
			foreach (var tile in tiles)
			{
				Tilemap.SetCell(tile, 0, atlasCoord);
			}
		}
	}
	
	public void ClearVisualization()
	{
		Tilemap.Clear();
	}
}
