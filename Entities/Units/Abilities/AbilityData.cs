using System;
using AKidsDream.Abilities.Effects;
using AKidsDream.GameBoard;
using AKidsDream.Managers.SaveSystems;
using AKidsDream.Units.Resources;
using Godot;

namespace AKidsDream.Abilities;

[GlobalClass]
public partial class AbilityData : Resource
{
	[Export] public Texture2D Icon;
	[Export] public StringName Name;
	[Export] public StringName Description;
	/// <summary>
	/// The pattern that determines which tiles an ability can select.
	/// </summary>
	[Export] public AccessFieldPattern ReachPattern;
	[Export] public Global.AtlasCoordsSprite ReachAtlasCoords = Global.AtlasCoordsSprite.TransparentTile;
	/// <summary>
	/// Contains the effect to apply to the selected tiles.
	/// </summary>
	[Export] public EffectData Effect;
	/// <summary>
	/// The Cost of the ability.
	/// </summary>
	[Export] public int Cost = 1;
	/// <summary>
	/// From which Pool the cost should be reduced.
	/// </summary>
	[Export] public StringName PoolName;
	// [Export] public StringName AnimationName;

	public (Vector2I atlasCoord, Vector2I[] tiles) GetReachVisualizationData(Unit source, Board board, Vector2I sourceTile)
	{
		var tiles = ReachPattern?.GetTiles(sourceTile, board, source.OwnerId) ?? Array.Empty<Vector2I>();
		return (Global.AtlasCoordsSpriteVectors[ReachAtlasCoords], tiles);
	}
}
