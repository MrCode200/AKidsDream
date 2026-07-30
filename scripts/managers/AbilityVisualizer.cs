using AKidsDream.Abilities;
using AKidsDream.GameBoard;
using AKidsDream.Globals;
using Godot;
using AKidsDream.Units;

namespace AKidsDream.Managers;

[GlobalClass]
public partial class AbilityVisualizer : Node
{
	[Export] public Board Board;
	[Export] public TileMapLayer ReachTilemap;
	[Export] public TileMapLayer EffectTilemap;

	public override void _Ready()
	{
		EffectTilemap.Scale = new Vector2(Global.TileMapScale, Global.TileMapScale);
		ReachTilemap.Scale = new Vector2(Global.TileMapScale, Global.TileMapScale);
	}

	public void ShowReachVisualization(Unit source, Vector2I sourceTile, AbilityData ability, bool clearPrevious = true)
	{
		if (clearPrevious) ClearReachTilemap();
		var visualizationData = ability.GetReachVisualizationData(source, Board, sourceTile);
		ShowVisualization(ReachTilemap, [visualizationData]);
	}
	
	public void ShowEffectVisualization(Unit source, Vector2I[] targetTiles, EffectData effect, bool clearPrevious = true)
	{
		if (clearPrevious) ClearEffectTilemap();
		var visualizationData = effect.GetEffectVisualizationData(source, Board, targetTiles);
		ShowVisualization(EffectTilemap, [visualizationData]);
	}
	
	public static void ShowVisualization(TileMapLayer tilemap, (Vector2I atlasCoord, Vector2I[] tiles)[] layers)
	{
		foreach (var (atlasCoord, tiles) in layers)
		{
			foreach (var tile in tiles)
			{
				// GD.Print($"Setting tile {tile} to {atlasCoord}");
				tilemap.SetCell(tile, 0, atlasCoord);
			}
		}
	}

	public void ClearTilemaps()
	{
		ClearEffectTilemap();
		ClearReachTilemap();
	}
	
	public void ClearEffectTilemap()
	{
		EffectTilemap.Clear();
	}
	
	public void ClearReachTilemap()
	{
		ReachTilemap.Clear();
	}
}
