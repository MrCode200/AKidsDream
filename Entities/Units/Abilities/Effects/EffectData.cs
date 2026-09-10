#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using AKidsDream.Common.Errors;
using AKidsDream.Common.Logging;
using AKidsDream.Common.Results;
using AKidsDream.Managers.SaveSystems;
using Godot;
using AKidsDream.Common.Components.TweenComponent.Resources;
using AKidsDream.Core.Managers.Audio;
using Godot.Collections;
using Serilog;

namespace AKidsDream.Abilities.Effects;

public enum ExecutionTrigger
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

    // Animation
    [ExportGroup("Animation")] [Export] public StringName? AnimationName;
    [Export] public bool ReplayIfAlreadyPlaying;


    [ExportGroup("Sound Settings")]
    private SoundEffectType _audioType = SoundEffectType.UnassignedSound;
    [Export] public SoundEffectType AudioType
    {
        get => _audioType;
        set
        {
            _audioType = value;
            NotifyPropertyListChanged();
        }
    }
    [Export] public bool PlaySound2D;
    private ExecutionTrigger _audioTrigger = ExecutionTrigger.Instant;
    [Export] public ExecutionTrigger AudioTrigger
    {
        get => _audioTrigger;
        set
        {
            _audioTrigger = value;
            NotifyPropertyListChanged();
        }
    }
    [Export] public float DelaySecondsAudio;
    [Export] public int TriggerValueAudio;

    [ExportGroup("Trigger")]
    private ExecutionTrigger _effectTrigger = ExecutionTrigger.Instant;

    [Export]
    public ExecutionTrigger EffectTrigger
    {
        get => _effectTrigger;
        set
        {
            _effectTrigger = value;
            NotifyPropertyListChanged();
        }
    }

    [Export] public bool BlockOnTrigger = true;
    [Export] public float DelaySecondsEffect;
    [Export] public int TriggerValueEffect;

    /// <summary>
    /// If false, <see cref="ExecuteAsync"/> will get be called for each target tile separately.
    /// </summary>
    [ExportGroup("")] [Export] public bool RunSequential;

    private static readonly ILogger Log = GameLogger.For(typeof(EffectData));

    public override void _ValidateProperty(Dictionary property)
    {
        var propertyName = property["name"].AsString();

        var show = true;
        switch (propertyName)
        {
            case nameof(DelaySecondsEffect):
                if (EffectTrigger != ExecutionTrigger.TimerEnd) show = false;
                break;
            case nameof(TriggerValueEffect):
                if (EffectTrigger is ExecutionTrigger.TimerEnd or ExecutionTrigger.Instant) show = false;
                break;
            
            case nameof(DelaySecondsAudio):
                if (AudioType == SoundEffectType.UnassignedSound || 
                    AudioTrigger != ExecutionTrigger.TimerEnd) show = false;
                break;
            case nameof(TriggerValueAudio):
                if (AudioType == SoundEffectType.UnassignedSound || 
                    AudioTrigger is ExecutionTrigger.TimerEnd or ExecutionTrigger.Instant) show = false;
                break;
        }
        
        if (!show)
        {
            property["usage"] = (int)PropertyUsageFlags.NoEditor;
            return;
        }
        
        var (disable, hint) = propertyName switch
        {
            nameof(AudioTrigger) => (AudioType == SoundEffectType.UnassignedSound,
                "To use AudioTrigger, AudioType must be set to a valid SoundEffectType."),
            
            _ => (false, "")
        };

        if (!string.IsNullOrEmpty(hint))
            property["hint_text"] = hint;
        
        if (disable)
            property["usage"] = (int)(property["usage"].AsInt32() | (long)PropertyUsageFlags.ReadOnly);
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
    /// <returns>Returns a <see cref="Result{EffectOutcome, EffectError}"/> indicating success or failure.</returns>
    public async Task<Result<EffectOutcome, EffectError>> ExecuteAsync(
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
                return await ExecuteEffectAsync(ctx, payload);
            }

            var outcomes = new List<EffectOutcome>(targetedTiles.Count);

            payload.AccumulatedTargets = [];
            foreach (var tile in targetedTiles)
            {
                payload.AccumulatedTargets.Add(tile);
                payload.ProcessingTiles = [tile];

                var effectResult = await ExecuteEffectAsync(ctx, payload);
                if (effectResult.IsFailure)
                    return effectResult;

                outcomes.Add(effectResult.Value);
            }

            return Result.Ok<EffectOutcome, EffectError>(new CompositeOutcome
            {
                Outcomes = outcomes
            });
        }
        catch (Exception exception)
        {
            Log.ForContext("IdTag", ctx.Caster.CasterId)
                .ForContext("NameTag", ctx.Caster.CasterName)
                .Here().Err(exception, "Error executing effect");

            return Result.Fail<EffectOutcome, EffectError>(
                new EffectError.ExecutionFailed(exception.Message));
        }
    }

    private async Task<Result<EffectOutcome, EffectError>> ExecuteEffectAsync(AbilityContext ctx, AbilityPayload payload)
    {
        PlayAnimationIfNeeded(ctx);
        PlaySoundIfNeeded(ctx, payload);

        // Trigger Logic (& UpdatePayload)
        if (EffectTrigger != ExecutionTrigger.Instant)
        {
            EventBus.Instance.EmitSignal(EventBus.SignalName.EffectTriggerStart, ctx.CasterNode, ctx.Ability, this);

            var waitTask = AwaitTriggerAsync(ctx, EffectTrigger, DelaySecondsEffect, TriggerValueEffect);
            UpdatePayload(ctx, payload);
            await waitTask;

            EventBus.Instance.EmitSignal(EventBus.SignalName.EffectTriggerEnd, ctx.CasterNode, ctx.Ability, this);
        }
        else
        {
            UpdatePayload(ctx, payload);
        }

        // Effect Application
        var affectedTiles = GetAffectedTiles(ctx, payload);

        EventBus.Instance.EmitSignal(EventBus.SignalName.EffectApplyStart, ctx.CasterNode, ctx.Ability, this);
        var result = ApplyEffect(ctx, payload, affectedTiles);
        EventBus.Instance.EmitSignal(EventBus.SignalName.EffectApplyEnd, ctx.CasterNode, ctx.Ability, this);

        return result;
    }

    private void PlayAnimationIfNeeded(AbilityContext ctx)
    {
        if (string.IsNullOrEmpty(AnimationName) || ctx.Caster.AnimComp == null) return;
        if (!ReplayIfAlreadyPlaying && ctx.Caster.AnimComp.GetCurrentAnimation() == AnimationName)
            return;

        ctx.Caster.AnimComp.PlayAnimation(AnimationName);
    }

    private async void PlaySoundIfNeeded(AbilityContext ctx, AbilityPayload payload)
    {
        if (AudioType == SoundEffectType.UnassignedSound) return;
        
        await AwaitTriggerAsync(ctx, AudioTrigger, DelaySecondsAudio, TriggerValueAudio);

        if (PlaySound2D)
        {
            if (payload.CurrentOrigin is { } origin)
            {
                AudioManager.Instance.PlayAudioAtLocation(AudioType, origin);
                return;
            }            
            
            Log.Here().Warn("Cannot play 2D sound effect without a valid origin. Fallback to non 2D Audio Playing");
        }
        
        AudioManager.Instance.PlayAudio(AudioType);
    }

    private async Task AwaitTriggerAsync(AbilityContext context, ExecutionTrigger trigger, float delaySeconds = 0, int triggerValue = 0)
    {
        var hasAnimComp = context.Caster.AnimComp != null;
        Log.Here().Debug("Awaiting trigger {Trigger} with value {TriggerValue}, hasAnimComp: {hasAnimComp}",
            trigger, triggerValue, hasAnimComp);

        switch (trigger)
        {
            case ExecutionTrigger.Instant:
                break;
            case ExecutionTrigger.TimerEnd:
                var timer = context.GameContext.GameManager.GetTree().CreateTimer(delaySeconds);
                await context.GameContext.GameManager.ToSignal(timer, SceneTreeTimer.SignalName.Timeout);
                break;
            case ExecutionTrigger.CastOnFrame when hasAnimComp:
                if (context.Caster.AnimComp!.HasReachedFrame(triggerValue))
                    break;
                Log.Here().Debug(
                    "Waiting for frame {TriggerValue}, current frame: {CurrentFrame}, animation: {Animation}",
                    triggerValue, context.Caster.AnimComp.GetCurrentFrame(),
                    context.Caster.AnimComp.GetCurrentAnimation());
                await context.Caster.AnimComp.WaitForTargetFrame(triggerValue);
                Log.Here().Debug("Reached frame {TriggerValue}", triggerValue);
                break;
            case ExecutionTrigger.CastAfterLoops when hasAnimComp:
                await context.Caster.AnimComp!.WaitForLoopCount(triggerValue);
                break;
            case ExecutionTrigger.CastAfterFrames when hasAnimComp:
                await context.Caster.AnimComp!.WaitForFrames(triggerValue);
                break;
            case ExecutionTrigger.CastOnLoop when hasAnimComp:
                await context.Caster.AnimComp!.WaitForTargetLoop(triggerValue);
                break;
            default:
                Log.Here().Warn("Invalid Trigger '{Trigger}' was requested in context: {hasAnimComp}",
                    trigger, hasAnimComp);
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


    public abstract Result<EffectOutcome, EffectError> ApplyEffect(AbilityContext context, AbilityPayload payload,
        Vector2I[] affectedTiles);

    public virtual void UpdatePayload(AbilityContext context, AbilityPayload payload)
    {
    }
}