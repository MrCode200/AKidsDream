using System.Collections.Generic;
using System.Linq;
using AKidsDream.Abilities;
using AKidsDream.Abilities.Effects;
using AKidsDream.GameBoard;
using AKidsDream.Managers.SaveSystems;
using Godot;
using AKidsDream.Common.Components;

namespace AKidsDream.Managers;

[GlobalClass]
public partial class AbilityVisualizer : Node
{
	[Export] public Board Board;
	[Export] public TileMapLayer ReachTilemap;
	[Export] public TileMapLayer EffectTilemap;
	[Export] public TileMapLayer NumberedTilemap;

	public override void _Ready()
	{
		EffectTilemap.Scale = new Vector2(Global.TileMapScale, Global.TileMapScale);
		ReachTilemap.Scale = new Vector2(Global.TileMapScale, Global.TileMapScale);
		NumberedTilemap.Scale = new Vector2(Global.TileMapScale, Global.TileMapScale);
	}

	public void ShowReachVisualization(AbilityContext context, AbilityPayload payload, AbilityData ability, bool clearPrevious = true)
	{
		if (clearPrevious) ClearReachTilemap();
		var visualizationData = ability.GetReachVisualizationData(context, payload);
		ShowVisualization(ReachTilemap, [visualizationData]);
	}
	
	public void ShowEffectVisualization(AbilityContext context, AbilityPayload payload, EffectData[] effects, bool clearPrevious = true)
	{
		if (clearPrevious) ClearEffectTilemap();
		var visualizationData = new (Vector2I atlasCoord, Vector2I[] tiles)[effects.Length];
		
		for (var i = 0; i < effects.Length; i++)
		{
			visualizationData[i] = effects[i].GetEffectVisualizationData(context, payload, true);
		}
		ShowVisualization(EffectTilemap, visualizationData);
		
		if (payload.AccumulatedTargets.Count > 1)
			ShowNumberedTilemap(payload.AccumulatedTargets);
	}
	
	public void ShowNumberedTilemap(IEnumerable<Vector2I> tiles, bool clearPrevious = true)
	{
		if (clearPrevious) ClearNumberedTilemap();
		
		var layers = tiles
			.Select((t, i) => ((Vector2I atlasCoord, Vector2I[] tiles))(new Vector2I(i, 0), [t]))
			.ToArray();
		
		ShowVisualization(NumberedTilemap, layers);
	}
	
	public static void ShowVisualization(TileMapLayer tilemap, (Vector2I atlasCoord, Vector2I[] tiles)[] layers)
	{
		foreach (var (atlasCoord, tiles) in layers)
		{
			foreach (var tile in tiles)
			{
				tilemap.SetCell(tile, 0, atlasCoord);
			}
		}
	}

	public void ClearTilemaps()
	{
		ClearEffectTilemap();
		ClearReachTilemap();
		ClearNumberedTilemap();
	}
	
	public void ClearEffectTilemap()
	{
		EffectTilemap.Clear();
	}
	
	public void ClearReachTilemap()
	{
		ReachTilemap.Clear();
	}
	
	public void ClearNumberedTilemap()
	{
		NumberedTilemap.Clear();
	}
}
