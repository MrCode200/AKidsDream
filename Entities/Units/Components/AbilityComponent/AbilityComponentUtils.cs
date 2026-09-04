#nullable enable
using System.Collections.Generic;
using AKidsDream.Abilities;
using AKidsDream.Abilities.Effects;
using AKidsDream.Commands;
using AKidsDream.GameBoard;
using Godot;

namespace AKidsDream.Common.Components.TweenComponent.Resources;

public enum CastFailureReason
{
    None,
    AbilityNotFound,
    CannotAfford,
    TilesOutOfRange,
    EffectExecutionFailed,
    InvalidTargetsSelected
}

public readonly record struct CastResult(bool Success, CastFailureReason FailureReason, EffectResult? EffectResult)
{
    public static CastResult Ok(EffectResult effectResult) => new(true, CastFailureReason.None, effectResult);
    public static CastResult Fail(CastFailureReason reason, EffectResult? effectResult = null) => new(false, reason, effectResult);
}

/// <summary>
/// Immutable context containing the environment for ability execution.
/// Contains references to the game state that should not be modified during ability execution.
/// </summary>
public partial class AbilityContext : Resource
{
    public required IAbilityCaster Caster { get; init; }
    public required Node CasterNode { get; init; }
    public required AbilityData Ability { get; init; }
    public PlayerId PlayerCasterId => Caster.OwnerId;
    public required GameContext GameContext { get; init; }
}

/// <summary>
/// Mutable payload containing the parameters for ability execution.
/// Contains data that can be modified during ability execution (targets, origin, results).
/// </summary>
public partial class AbilityPayload : Resource
{
    /// <summary>
    /// Accumulated targets from incremental selection (e.g., during AddAbilityTargetCommand).
    /// Cost modifiers can use this to calculate cost based on all tiles selected so far.
    /// </summary>
    public List<Vector2I> AccumulatedTargets = [];
    public List<Vector2I> ProcessingTiles = [];
    // public Vector2I[] AdditionalReachTiles = [];
    public required AbilityState State = new();
    public required Vector2I CurrentOrigin { get; set; }

    public AbilityPayload Copy()
    {
        return new AbilityPayload
        {
            ProcessingTiles = [.. ProcessingTiles],
            AccumulatedTargets = [.. AccumulatedTargets],
            // AdditionalReachTiles = (Vector2I[])AdditionalReachTiles.Clone(),
            State = State.Copy(),
            CurrentOrigin = CurrentOrigin,
        };
    }
    
    public void SetValuesTo(AbilityPayload payload)
    {
        ProcessingTiles = payload.ProcessingTiles;
        AccumulatedTargets = payload.AccumulatedTargets;
        // AdditionalReachTiles = payload.AdditionalReachTiles;
        CurrentOrigin = payload.CurrentOrigin;

        State.SetValuesTo(payload.State);
    }
}
public class AbilityState
{
    public Dictionary<StringName, int> Counters { get; private set; } = new();
    public Dictionary<StringName, bool> Flags { get; private set; } = new();
    
    public int GetCounter(StringName key, int defaultValue = 0) => 
        Counters.GetValueOrDefault(key, defaultValue);
    
    public void SetCounter(StringName key, int value) => 
        Counters[key] = value;
    
    public bool GetFlag(StringName key, bool defaultValue = false) => 
        Flags.GetValueOrDefault(key, defaultValue);
    
    public void SetFlag(StringName key, bool value) => 
        Flags[key] = value;
    
    public AbilityState Copy()
    {
        return new AbilityState
        {
            Counters = new Dictionary<StringName, int>(Counters),
            Flags = new Dictionary<StringName, bool>(Flags)
        };
    }
    
    public void SetValuesTo(AbilityState state)
    {
        Counters = new Dictionary<StringName, int>(state.Counters);
        Flags = new Dictionary<StringName, bool>(state.Flags);
    }
}