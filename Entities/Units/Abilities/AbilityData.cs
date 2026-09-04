#nullable enable
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using AKidsDream.Abilities.CostModifiers;
using AKidsDream.Abilities.Effects;
using AKidsDream.Managers.SaveSystems;
using AKidsDream.Common.Components.TweenComponent.Resources;
using Godot;

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
    [Export] public required AccessFieldPattern? ReachPattern;

    [Export] public Global.AtlasCoordsSprite ReachAtlasCoords = Global.AtlasCoordsSprite.TransparentTile;

    /// <summary>
    /// Contains the effects to apply to the selected tiles in insertion order.
    /// </summary>
    [Export] public EffectData[] Effects = [];

    [Export] public int BaseCost = 1;
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
    
    [Signal] public delegate void AbilityCastEventHandler(AbilityData ability, EffectResult result);

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
    /// <returns>Returns a CompositeResult containing all individual effect results</returns>
    public async Task<(EffectResult Result, AbilityPayload Payload)> CastAsync(
        AbilityContext context,
        List<Vector2I> targetedTiles,
        AbilityState? state
    )
    {
        state ??= new AbilityState();
        var payload = new AbilityPayload
        {
            CurrentOrigin = context.Caster.TileLocation,
            State = state
        };
        
        var effectResults = new EffectResult[Effects.Length];
        for (var i = 0; i < Effects.Length; i++)
        {
            var effectResult = await Effects[i].ExecuteAsync(context, targetedTiles, payload);
            effectResults[i] = effectResult;

            if (effectResult is ErrorResult) return (effectResult, payload);
        }
        
        var result = new CompositeResult { Results = effectResults };
        EmitSignal(SignalName.AbilityCast, this, result);
        return (new CompositeResult { Results = effectResults }, payload);
    }

    // -- UTIL METHODS --
    
    public (Vector2I atlasCoord, Vector2I[] tiles) GetReachVisualizationData(
        AbilityContext context,
        AbilityPayload payload)
    {
        if (ReachPattern is null)
            return (Global.AtlasCoordsSpriteVectors[Global.AtlasCoordsSprite.TransparentTile],
                []);

        var tiles = new List<Vector2I>();
        var allReachTiles = new[] { payload.CurrentOrigin };
        // allReachTiles = allReachTiles.Concat(payload.AccumulatedTargets).ToArray();

        foreach (var t in allReachTiles)
        {
            tiles.AddRange(ReachPattern.GetTiles(
                t,
                context.GameContext.Board,
                context.PlayerCasterId,
                context.GameContext.PlayerTeamRegistry) ?? []);
        }

        return (Global.AtlasCoordsSpriteVectors[ReachAtlasCoords], tiles.Distinct().ToArray());
    }

    public int GetCost(AbilityContext context, AbilityPayload payload)
    {
        return CostMod?.GetCost(BaseCost, context, payload) ?? BaseCost;
    }
    
    public bool CanReplenishPool()
    {
        if (CostMod == null) return BaseCost < 0;
        return CostMod?.CanReplenishPool() ?? false;
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
    
    public bool CanAfford(int balance, AbilityContext context, AbilityPayload payload)
    {
        return GetCost(context, payload) <= balance;
    }

    /// <summary>
    /// Checks if a single tile is within reach from the specified origin.
    /// Returns true if ReachPattern is null.
    /// </summary>
    public bool IsTileInReach(
        AbilityContext context,
        Vector2I tile,
        Vector2I origin
    )
    {
        return AllTilesInReach(context, [tile], origin);
    }

    /// <summary>
    /// Checks if all specified tiles are within reach from the specified origin.
    /// Returns true if ReachPattern is null.
    /// </summary>
    public bool AllTilesInReach(
        AbilityContext context,
        IEnumerable<Vector2I> tiles,
        Vector2I origin
    )
    {
        if (ReachPattern is null) return true;

        var validTiles = ReachPattern.GetTiles(
            origin,
            context.GameContext.Board,
            context.PlayerCasterId,
            context.GameContext.PlayerTeamRegistry
        );
        return tiles.All(validTiles.Contains);
    }

    /// <summary>
    /// Updates the payload for a single batch effect: all targeted tiles are
    /// processed together in one call, matching EffectData.Execute's non-sequential branch.
    /// </summary>
    public static void UpdatePayloadBatch(
        EffectData effect,
        AbilityContext context,
        List<Vector2I> targetedTiles,
        AbilityPayload payload)
    {
        payload.ProcessingTiles = targetedTiles;
        payload.AccumulatedTargets = targetedTiles;
        effect.UpdatePayload(context, payload);
    }

    /// <summary>
    /// Updates the payload sequentially tile by tile for a sequential effect,
    /// checking reach for each tile if requested.
    /// </summary>
    public bool TryUpdatePayloadSequential(
        EffectData effect,
        AbilityContext context,
        List<Vector2I> targetedTiles,
        AbilityPayload payload,
        bool checkReach,
        out CastFailureReason reason)
    {
        reason = CastFailureReason.None;
        payload.AccumulatedTargets = [];

        foreach (var tile in targetedTiles)
        {
            if (checkReach && !IsTileInReach(context, tile, payload.CurrentOrigin))
            {
                reason = CastFailureReason.TilesOutOfRange;
                return false;
            }

            payload.AccumulatedTargets.Add(tile);
            payload.ProcessingTiles = [tile];
            effect.UpdatePayload(context, payload);
        }

        return true;
    }

    /// <summary>
    /// Main validation dispatcher for the ability. Validates target count and reachability,
    /// and simulates each effect's payload update (sequential or batch) in insertion order.
    /// Does not check pool costs or affordability.
    /// </summary>
    /// <param name="context">The context for the cast.</param>
    /// <param name="targetedTiles">The tiles selected for the ability.</param>
    /// <param name="payload">The resulting payload populated with updated state, origin, and targets upon successful validation.</param>
    /// <param name="reason">The failure reason if validation fails.</param>
    /// <param name="balance">The balance to check the cost against if null check is skipped.</param>
    /// <param name="state">Optional ability state. If null, a new AbilityState instance is used.</param>
    /// <param name="initialOrigin">Optional initial origin. If null, context.Caster.TileLocation is used.</param>
    /// <returns>True if the cast parameters and target reachability are valid; otherwise false.</returns>
    public bool ValidateCast(
        AbilityContext context,
        List<Vector2I> targetedTiles,
        [NotNullWhen(true)] out AbilityPayload? payload,
        out CastFailureReason reason,
        int? balance = null,
        AbilityState? state = null,
        Vector2I? initialOrigin = null)
    {
        reason = CastFailureReason.None;
        payload = null;

        if (!HasValidTargetCount(targetedTiles))
        {
            reason = CastFailureReason.InvalidTargetsSelected;
            return false;
        }

        var abilityState = state?.Copy() ?? new AbilityState();
        var origin = initialOrigin ?? context.Caster.TileLocation;

        payload = new AbilityPayload
        {
            CurrentOrigin = origin,
            ProcessingTiles = targetedTiles,
            AccumulatedTargets = targetedTiles,
            State = abilityState
        };

        for (var i = 0; i < Effects.Length; i++)
        {
            var effect = Effects[i];
            var isFirst = i == 0;

            if (effect.RunSequential)
            {
                // Sequential effect checks reachability for each tile itself
                if (!TryUpdatePayloadSequential(effect, context, targetedTiles, payload, isFirst, out reason))
                {
                    payload = null;
                    return false;
                }
            }
            else
            {
                // Batch effect checks reachability for the origin
                if (isFirst && !AllTilesInReach(context, targetedTiles, payload.CurrentOrigin))
                {
                    reason = CastFailureReason.TilesOutOfRange;
                    payload = null;
                    return false;
                }

                UpdatePayloadBatch(effect, context, targetedTiles, payload);
            }
        }
        
        if (balance != null && !CanAfford(balance.Value, context, payload))
        {
            reason = CastFailureReason.CannotAfford;
            return false;
        }
        return true;
    }
}