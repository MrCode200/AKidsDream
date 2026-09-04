#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using AKidsDream.Common.Logging;
using AKidsDream.Managers.SaveSystems;
using Godot;
using AKidsDream.Common.Components.TweenComponent.Resources;
using Godot.Collections;
using Serilog;

namespace AKidsDream.Abilities.Effects;

public enum EffectTrigger
{
    Instant = 1 << 0, // NOTE: This doesn't emit the Signals for TriggerStart and TriggerEnd
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
    /// If false, <see cref="ExecuteAsync"/> will get be called for each target tile separately.
    /// </summary>
    [Export] public bool RunSequential;

    // Animation
    [ExportGroup("Animation")] [Export] public StringName? AnimationName;
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

    [Export] public bool BlockOnTrigger = true;
    [Export] public float DelaySeconds;
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
    public async Task<EffectResult> ExecuteAsync(
        AbilityContext ctx,
        List<Vector2I> targetedTiles,
        AbilityPayload payload
    )
    {
        EffectResult finalResult = new CompositeResult { Results = [] };

        try
        {
            if (!RunSequential)
            {
                payload.ProcessingTiles = targetedTiles;
                payload.AccumulatedTargets = targetedTiles;
                return await ExecuteEffectAsync(ctx, payload);
            }

            var index = 0;
            var results = new EffectResult[targetedTiles.Count];

            payload.AccumulatedTargets = [];
            foreach (var tile in targetedTiles)
            {
                payload.AccumulatedTargets.Add(tile);
                payload.ProcessingTiles = [tile];

                results[index++] = await ExecuteEffectAsync(ctx, payload);
            }

            finalResult = new CompositeResult { Results = results };
            return finalResult;
        }
        catch (Exception exception)
        {
            Log.ForContext("IdTag", ctx.Caster.CasterId)
                .ForContext("NameTag", ctx.Caster.CasterName)
                .Here().Err(exception, "Error executing effect");

            return finalResult;
        }
    }

    private async Task<EffectResult> ExecuteEffectAsync(AbilityContext ctx, AbilityPayload payload)
    {
        PlayAnimationIfNeeded(ctx);

        // Trigger Logic (& UpdatePayload)
        if (_trigger != EffectTrigger.Instant)
        {
            EventBus.Instance.EmitSignal(EventBus.SignalName.EffectTriggerStart, ctx.CasterNode, ctx.Ability, this);

            var waitTask = AwaitTriggerAsync(ctx);
            UpdatePayload(ctx, payload);
            await waitTask;

            EventBus.Instance.EmitSignal(EventBus.SignalName.EffectTriggerEnd, ctx.CasterNode, ctx.Ability, this);
        }
        else
        {
            UpdatePayload(ctx, payload);
        }

        var affectedTiles = GetAffectedTiles(ctx, payload);

        EventBus.Instance.EmitSignal(EventBus.SignalName.EffectApplyStart, ctx.CasterNode, ctx.Ability, this);
        var result = ApplyEffect(ctx, payload, affectedTiles);
        EventBus.Instance.EmitSignal(EventBus.SignalName.EffectApplyEnd, ctx.CasterNode, ctx.Ability, this, result);

        return result;
    }

    private void PlayAnimationIfNeeded(AbilityContext ctx)
    {
        if (string.IsNullOrEmpty(AnimationName) || ctx.Caster.AnimComp == null) return;
        if (!ReplayIfAlreadyPlaying && ctx.Caster.AnimComp.GetCurrentAnimation() == AnimationName)
            return;

        ctx.Caster.AnimComp.PlayAnimation(AnimationName);
    }

    private async Task AwaitTriggerAsync(AbilityContext context)
    {
        var hasAnimComp = context.Caster.AnimComp != null;
        Log.Here().Debug("Awaiting trigger {Trigger} with value {TriggerValue}, hasAnimComp: {hasAnimComp}",
            _trigger, TriggerValue, hasAnimComp);

        switch (_trigger)
        {
            case EffectTrigger.Instant:
                break;
            case EffectTrigger.TimerEnd:
                var timer = context.GameContext.GameManager.GetTree().CreateTimer(DelaySeconds);
                await context.GameContext.GameManager.ToSignal(timer, SceneTreeTimer.SignalName.Timeout);
                break;
            case EffectTrigger.CastOnFrame when hasAnimComp:
                if (context.Caster.AnimComp!.HasReachedFrame(TriggerValue))
                    break;
                Log.Here().Debug("Waiting for frame {TriggerValue}, current frame: {CurrentFrame}, animation: {Animation}",
                    TriggerValue, context.Caster.AnimComp.GetCurrentFrame(), context.Caster.AnimComp.GetCurrentAnimation());
                await context.Caster.AnimComp.WaitForTargetFrame(TriggerValue);
                Log.Here().Debug("Reached frame {TriggerValue}", TriggerValue);
                break;
            case EffectTrigger.CastAfterLoops when hasAnimComp:
                await context.Caster.AnimComp!.WaitForLoopCount(TriggerValue);
                break;
            case EffectTrigger.CastAfterFrames when hasAnimComp:
                await context.Caster.AnimComp!.WaitForFrames(TriggerValue);
                break;
            case EffectTrigger.CastOnLoop when hasAnimComp:
                await context.Caster.AnimComp!.WaitForTargetLoop(TriggerValue);
                break;
            default:
                Log.Here().Warn("Invalid Trigger '{Trigger}' was requested in context: {hasAnimComp}",
                    _trigger, hasAnimComp);
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
                .SelectMany(tile => EffectPattern.GetTiles(
                    tile,
                    context.GameContext.Board,
                    context.PlayerCasterId,
                    context.GameContext.PlayerTeamRegistry
                ))
                .ToArray();

        Log.ForContext<EffectData>().Here().Err("EffectPattern is null {EffectType}", GetType().Name);
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


    public abstract EffectResult ApplyEffect(AbilityContext context, AbilityPayload payload, Vector2I[] affectedTiles);

    public virtual void UpdatePayload(AbilityContext context, AbilityPayload payload)
    {
    }
}