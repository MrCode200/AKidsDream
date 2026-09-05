#nullable enable
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using AKidsDream.Abilities.CostModifiers;
using AKidsDream.Abilities.Effects;
using AKidsDream.Common.Components.TweenComponent.Resources;
using AKidsDream.Common.Errors;
using AKidsDream.Common.Results;
using AKidsDream.Managers.SaveSystems;
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

    [Signal]
    public delegate void AbilityCastEventHandler(AbilityData ability);

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
    /// </summary>
    /// <param name="context">The context, containing unmodifiable classes</param>
    /// <param name="targetedTiles">The tiles the User selected in insertion order</param>
    /// <param name="state">The state of the ability (Counters etc.)</param>
    /// <returns>Returns a Result containing the composite outcome and payload, or CastError.</returns>
    public async Task<Result<(CompositeOutcome Outcomes, AbilityPayload Payload), CastError>> CastAsync(
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

        var outcomes = new List<EffectOutcome>(Effects.Length);
        for (var i = 0; i < Effects.Length; i++)
        {
            var effectResult = await Effects[i].ExecuteAsync(context, targetedTiles, payload);
            if (effectResult.IsFailure)
                return Result.Fail<(CompositeOutcome, AbilityPayload), CastError>(new CastError.EffectFailed(effectResult.Error));

            outcomes.Add(effectResult.Value);
        }

        var compositeOutcome = new CompositeOutcome(outcomes, flatten: true) { Caster = context.Caster };
        EmitSignal(SignalName.AbilityCast, this);
        return Result.Ok<(CompositeOutcome, AbilityPayload), CastError>((compositeOutcome, payload));
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
    /// Checks that no duplicate tile counted exceeds <see cref="MaxDuplicateTargets"/>.
    /// </summary>
    /// <param name="targetTiles">The Tiles the User selected.</param>
    public Result<CastError> ValidateTargetCount(List<Vector2I> targetTiles)
    {
        var count = targetTiles.Count;
        if (count < MinTargets || count > MaxTargets)
            return Result.Fail<CastError>(new CastError.InvalidTargetCount(MinTargets, MaxTargets, count));

        var duplicates = targetTiles.GroupBy(t => t)
            .Select(g => new { Value = g.Key, Count = g.Count() })
            .ToArray();

        foreach (var duplicate in duplicates)
        {
            if (duplicate.Count > _maxDuplicateTargets)
                return Result.Fail<CastError>(new CastError.MaxDuplicateTargetsExceeded(duplicate.Value, _maxDuplicateTargets, duplicate.Count));
        }

        return Result.Ok<CastError>();
    }

    public bool HasValidTargetCount(List<Vector2I> targetTiles) =>
        ValidateTargetCount(targetTiles).IsSuccess;

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
        Vector2I? origin
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
        Vector2I? origin
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
    public Result<CastError> TryUpdatePayloadSequential(
        EffectData effect,
        AbilityContext context,
        List<Vector2I> targetedTiles,
        AbilityPayload payload
    )
    {
        payload.AccumulatedTargets = [];

        foreach (var tile in targetedTiles)
        {
            if (!IsTileInReach(context, tile, payload.CurrentOrigin))
            {
                return Result.Fail<CastError>(new CastError.TargetOutOfRange(tile, payload.CurrentOrigin));
            }

            payload.AccumulatedTargets.Add(tile);
            payload.ProcessingTiles = [tile];
            effect.UpdatePayload(context, payload);
        }

        return Result.Ok<CastError>();
    }

    /// <summary>
    /// Main validation dispatcher for the ability. Validates target count and reachability,
    /// and simulates each effect's payload update (sequential or batch) in insertion order.
    /// Does not check pool costs or affordability unless balance is provided.
    /// </summary>
    public Result<AbilityPayload, CastError> ValidateCast(
        AbilityContext context,
        List<Vector2I> targetedTiles,
        int? balance = null,
        AbilityState? state = null,
        Vector2I? initialOrigin = null)
    {
        var targetCountResult = ValidateTargetCount(targetedTiles);
        if (targetCountResult.IsFailure)
            return Result.Fail<AbilityPayload, CastError>(targetCountResult.Error);

        var abilityState = state?.Copy() ?? new AbilityState();
        var origin = initialOrigin ?? context.Caster.TileLocation;

        var payload = new AbilityPayload
        {
            CurrentOrigin = origin,
            ProcessingTiles = targetedTiles,
            AccumulatedTargets = targetedTiles,
            State = abilityState
        };

        for (var i = 0; i < Effects.Length; i++)
        {
            var effect = Effects[i];

            if (effect.RunSequential)
            {
                var seqResult = TryUpdatePayloadSequential(effect, context, targetedTiles, payload);
                if (seqResult.IsFailure)
                    return Result.Fail<AbilityPayload, CastError>(seqResult.Error);
            }
            else
            {
                if (targetedTiles.Count > 0 && !AllTilesInReach(context, targetedTiles, payload.CurrentOrigin))
                {
                    var invalidTile = targetedTiles.FirstOrDefault(t => !IsTileInReach(context, t, payload.CurrentOrigin));
                    return Result.Fail<AbilityPayload, CastError>(new CastError.TargetOutOfRange(invalidTile, payload.CurrentOrigin));
                }

                UpdatePayloadBatch(effect, context, targetedTiles, payload);
            }
        }

        if (balance != null && !CanAfford(balance.Value, context, payload))
        {
            var cost = GetCost(context, payload);
            return Result.Fail<AbilityPayload, CastError>(new CastError.CannotAfford(PoolName.ToString(), cost, balance.Value));
        }

        return Result.Ok<AbilityPayload, CastError>(payload);
    }
}
