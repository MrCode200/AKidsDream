#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using AKidsDream.Common.Logging;
using AKidsDream.Managers.SaveSystems;
using Godot;
using AKidsDream.Units.Resources.Components;
using Godot.Collections;
using Serilog;

namespace AKidsDream.Abilities.Effects;

public enum EffectTrigger
{
	Instant = 1 << 0,
	TimerEnd = 1 << 1,
	CastOnFrame = 1 << 2,
	CastOnLoop = 1 << 3,
	CastAfterFrames = 1 << 4,
	CastAfterLoops = 1 << 5
}

[GlobalClass]
[Tool]
public abstract partial class EffectData : Resource
{
	[Export] public AccessFieldPattern? EffectPattern;
	[Export] public Global.AtlasCoordsSprite EffectAtlasCoords;

	/// <summary>
	/// If false, <see cref="Execute"/> will get be called for each target tile separately.
	/// </summary>
	[Export] public bool RunSequential;

	// Animation
	[ExportGroup("Animation")]
	[Export] public StringName? AnimationName;
	[Export] public bool ReplayIfAlreadyPlaying;
	
	private EffectTrigger _trigger = EffectTrigger.Instant;
	[ExportGroup("Trigger")]
	[Export]
	public EffectTrigger Trigger
	{
		get => _trigger;
		set
		{
			_trigger = value;
			NotifyPropertyListChanged();
		}
	}

	[Export] public float DelaySeconds { get; set; }
	[Export] public int TriggerValue;
	
	private static readonly ILogger Log = GameLogger.For(typeof(EffectData));

	public override void _ValidateProperty(Dictionary property)
	{
		var propertyName = property["name"].AsStringName();

		var show = true;
		switch (propertyName)
		{
			case nameof(DelaySeconds):
				if (_trigger != EffectTrigger.TimerEnd) show = false;
				break;
			case nameof(TriggerValue):
				if (_trigger is EffectTrigger.TimerEnd or EffectTrigger.Instant) show = false;
				break;
		}


		if (!show)
			property["usage"] = (int)PropertyUsageFlags.NoEditor;
	}


	// -- LOGIC --
	/// <summary>
	/// Checks if the number of Tiles the User selected is valid.
	/// If AllowDuplicateTiles is false, all Tiles must be unique.
	/// Calls <see cref="ApplyEffect"/> if the number of Tiles is valid.
	/// </summary>
	/// <param name="ctx">The context, containing unmodifiable classes</param>
	/// <param name="targetedTiles">The tiles the User selected in insertion order</param>
	/// <param name="payload">The payload, containing modifiable data</param>
	/// <remarks>Note: The execution passed may be modified during execution.</remarks>
	/// <returns>Returns an <see cref="EffectResult"/> which contains data of what effect did what.</returns>
	public async Task<EffectResult> Execute(
		AbilityContext ctx,
		List<Vector2I> targetedTiles,
		AbilityPayload payload
	)
	{
		try
		{
			if (!RunSequential)
			{
				payload.ProcessingTiles = targetedTiles;
				payload.AccumulatedTargets = targetedTiles;
				TryPlayAnimation(ctx);
				await HandlePayload(ctx, payload);
				return ApplyEffect(ctx, payload);
			}

			var results = new EffectResult[targetedTiles.Count];
			var index = 0;

			payload.AccumulatedTargets = [];
			foreach (var tile in targetedTiles)
			{
				payload.AccumulatedTargets.Add(tile);
				payload.ProcessingTiles = [tile];

				TryPlayAnimation(ctx);
				await HandlePayload(ctx, payload);
			
				results[index++] = ApplyEffect(ctx, payload);
			}

			return new CompositeResult { Results = results };
		}
		catch (Exception exception)
		{
			Log.ForContext("UnitId", ctx.Source.UnitId)
				.ForContext("UnitName", ctx.Source.Name)
				.Here().Error(exception, "Error executing effect");
			return new CompositeResult { Results = [] };
		}
	}

	private void TryPlayAnimation(AbilityContext ctx)
	{
		if (string.IsNullOrEmpty(AnimationName)) return;
		if (!ReplayIfAlreadyPlaying && ctx.Source.AnimationC.CurrentAnimation() == AnimationName)
			return;
		
		ctx.Source.AnimationC.PlayAnimation(AnimationName);
	}

	private async Task HandlePayload(AbilityContext ctx, AbilityPayload payload)
	{
		try
		{
			var waitTask = WaitForTrigger(ctx);
			UpdatePayload(ctx, payload);
			await waitTask;
		}
		catch (Exception e)
		{
			Log.ForContext("UnitId", ctx.Source.UnitId)
				.ForContext("UnitName", ctx.Source.Name)
				.Here().Error(e, "Error waiting for trigger");
		}
	}


	private async Task WaitForTrigger(AbilityContext context)
	{
		switch (_trigger)
		{
			case EffectTrigger.Instant:
				break;
			case EffectTrigger.TimerEnd:
				var timer = context.Source.GetTree().CreateTimer(DelaySeconds);
				await context.Source.ToSignal(timer, SceneTreeTimer.SignalName.Timeout);
				break;
			case EffectTrigger.CastOnFrame:
				if (context.Source.AnimationC.HasReachedFrame(TriggerValue))
					break;
				await context.Source.AnimationC.WaitForTargetFrame(TriggerValue);
				break;
			case EffectTrigger.CastAfterLoops:
				await context.Source.AnimationC.WaitForLoopCount(TriggerValue);
				break;
			case EffectTrigger.CastAfterFrames:
				await context.Source.AnimationC.WaitForFrames(TriggerValue);
				break;
			case EffectTrigger.CastOnLoop:
				await context.Source.AnimationC.WaitForTargetLoop(TriggerValue);
				break;
		}

	}

	// -- UTILS --

	/// <summary>
	/// Returns the Tiles that will be affected by the effect.
	/// <param name="context">The context, containing unmodifiable classes</param>
	/// <param name="payload">The payload, containing modifiable data</param>
	/// <returns>An array of <see cref="Vector2I"/> which is the TileData.TileLocation</returns>
	/// </summary>
	protected virtual Vector2I[] GetAffectedTiles(
		AbilityContext context,
		AbilityPayload payload,
		bool useAccumulatedTiles = false
	)
	{
		var tiles = useAccumulatedTiles ? payload.AccumulatedTargets : payload.ProcessingTiles;
		if (EffectPattern != null)
			return tiles
				.SelectMany(tile => EffectPattern.GetTiles(tile, context.Board, context.CasterId))
				.ToArray();

		Log.ForContext<EffectData>().Here().Error("EffectPattern is null {EffectType}", GetType().Name);
		return [];
	}

	/// <summary>
	/// Returns the atlas coordinates and tiles that will be used to visualize the effect.
	/// <param name="context">The context, containing unmodifiable classes</param>
	/// <param name="payload">The payload, containing modifiable data</param>
	/// </summary>
	public virtual (Vector2I atlasCoord, Vector2I[] tiles) GetEffectVisualizationData(
		AbilityContext context,
		AbilityPayload payload,
		bool useAccumulatedTiles = false
	)
	{
		// TODO: Handle visualization of duplicate tiles
		var tiles = GetAffectedTiles(context, payload, useAccumulatedTiles);
		return (Global.AtlasCoordsSpriteVectors[EffectAtlasCoords], tiles);
	}


	public abstract EffectResult ApplyEffect(AbilityContext context, AbilityPayload payload);

	public virtual void UpdatePayload(AbilityContext context, AbilityPayload payload) { }
}
