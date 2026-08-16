#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AKidsDream.Abilities.CostModifiers;
using AKidsDream.Abilities.Effects;
using AKidsDream.Managers.SaveSystems;
using AKidsDream.Units.Resources.Components;
using Godot;
using Godot.Collections;

namespace AKidsDream.Abilities;

[GlobalClass]
[Tool]
public partial class AbilityData : Resource
{
	[Export] public Texture2D? Icon;
	[Export] public StringName Name = "AbilityName";
	[Export] public StringName? Description;
	/// <summary>
	/// The pattern that determines which tiles an ability can select.
	/// </summary>
	[Export] public required AccessFieldPattern ReachPattern;
	[Export] public Global.AtlasCoordsSprite ReachAtlasCoords = Global.AtlasCoordsSprite.TransparentTile;
	/// <summary>
	/// Contains the effects to apply to the selected tiles in insertion order.
	/// </summary>
	[Export] public EffectData[] Effects = [];

	[Export] public int Cost = 1;
	[Export] public CostModifier? CostMod;

	/// <summary>
	/// From which Pool the cost should be reduced.
	/// </summary>
	[Export] public required StringName PoolName;

	/// <summary>
	/// The minimum number of Tiles the User needs to select.
	/// </summary>
	private int _minTargets = 1;
	
	[Export]
	public int MinTargets
	{
		get => _minTargets;
		set
		{
			// Clamp value to be at most MaxTargets
			_minTargets = value;
			if (_minTargets > MaxTargets)
				MaxTargets = _minTargets;
		}
	}

	/// <summary>
	/// The maximum number of Tiles the User needs to select.
	/// </summary>
	private int _maxTargets = 1;

	[Export]
	public int MaxTargets
	{
		get => _maxTargets;
		set
		{
			// Clamp value to be at least MinTargets
			_maxTargets = value;
			if (_maxTargets < MinTargets)
				MinTargets = _maxTargets;
		}
	}

	/// <summary>
	/// Whether the User can select the same Tile multiple times.
	/// </summary>
	private int _maxDuplicateTargets = 1;

	[Export]
	public int MaxDuplicateTargets
	{
		get => _maxDuplicateTargets;
		set
		{
			if (value < 1) value = 1;
			_maxDuplicateTargets = value;
		}
	}
	
	// -- LOGIC --
	/// <summary>
	/// Updates the payload by calling all effects' UpdatePayload in insertion order.
	/// This allows effects to modify the payload (origin, counters, flags, etc.) in sequence.
	/// </summary>
	/// <param name="context">The context, containing unmodifiable classes</param>
	/// <param name="payload">The payload, containing modifiable data</param>
	public void UpdatePayload(AbilityContext context, AbilityPayload payload)
	{
		foreach (var effect in Effects)
		{
			effect.UpdatePayload(context, payload);
		}
	}

	/// <summary>
	/// Casts the ability by executing all effects in insertion order.
	/// Each effect is calculated independently and returns its individual EffectResult.
	/// </summary>
	/// <param name="context">The context, containing unmodifiable classes</param>
	/// <param name="targetedTiles">The tiles the User selected in insertion order</param>
	/// <param name="state">The state of the ability (Counters etc.)</param>
	/// <param name="payload">The payload, containing modifiable data</param>
	/// <returns>Returns a CompositeResult containing all individual effect results</returns>
	public async Task<(EffectResult Result, AbilityPayload Payload)> CastAsync(
		AbilityContext context,
		List<Vector2I> targetedTiles,
		AbilityState state
	)
	{
		var effectResults = new EffectResult[Effects.Length];

		var payload = new AbilityPayload
		{
			CurrentOrigin = context.Source.TileLocation,
			ProcessingTiles = targetedTiles,
			AccumulatedTargets = targetedTiles,
			State = state
		};

		for (var i = 0; i < Effects.Length; i++)
		{
			var effectResult = await Effects[i].ExecuteAsync(context, targetedTiles, payload);
			effectResults[i] = effectResult;

			if (effectResult is ErrorResult) return (effectResult, payload);
		}

		return (new CompositeResult { Results = effectResults }, payload);
	}

	public (Vector2I atlasCoord, Vector2I[] tiles) GetReachVisualizationData(AbilityContext context, AbilityPayload payload)
	{
		Vector2I[] tiles = [];
		var allReachTiles = new[] { payload.CurrentOrigin };
			// .Concat(payload.AdditionalReachTiles);
		
		tiles = allReachTiles.Aggregate(tiles, (current, t) => [
			.. current,
			.. ReachPattern.GetTiles(t, context.Board, context.CasterId) ?? []
		])
			.Distinct()
			.ToArray();
		
		return (Global.AtlasCoordsSpriteVectors[ReachAtlasCoords], tiles);
	}
	
	public int GetCost(AbilityContext context, AbilityPayload payload)
	{
		return CostMod?.GetCost(Cost, context, payload) ?? Cost;
	}
	
	// -- VALIDATION --

	/// <summary>
	/// Checks if the number of Tiles the User selected is valid.
	/// Cheks that no duplicate tile counted exceeds <see cref="MaxDuplicateTargets"/>.
	/// </summary>
	/// <param name="targetTiles">The Tiles the User selected.</param>
	public bool HasValidTargetCount(List<Vector2I> targetTiles)
	{
		var count = targetTiles.Count;
		if (count < MinTargets || count > MaxTargets)
			return false;

		var duplicates = targetTiles.GroupBy(t => t)
			.Select(g => new { Value = g.Key, Count = g.Count() })
			.ToArray();
		
		foreach (var duplicate in duplicates)
		{
			if (duplicate.Count > _maxDuplicateTargets) return false;
		}

		return true;
	}
}
