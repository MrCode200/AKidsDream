#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using AKidsDream.Abilities;
using AKidsDream.Abilities.Effects;
using AKidsDream.Common.Components.TweenComponent.Resources;
using AKidsDream.Common.Errors;
using AKidsDream.Common.Logging;
using AKidsDream.Common.Results;
using AKidsDream.GameBoard;
using AKidsDream.Managers.SaveSystems;
using Godot;
using Godot.Collections;
using Serilog;

namespace AKidsDream.Common;

[GlobalClass]
[Tool]
public partial class AbilityComponent : Node
{
    private ILogger _log = GameLogger.For<AbilityComponent>();
    public Unit Unit => (Unit)GetParent();

    /// <summary>
    /// Contains the pool data for each ability pool.
    /// Where the key is the pool name and the value is the PoolData resource.
    /// </summary>
    [Export] public Array<PoolData> InitialPoolDatas = [];

    public readonly System.Collections.Generic.Dictionary<StringName, PoolData> Pools = new();

    [Export] public Array<AbilityData> InitialAbilityDatas = [];
    public readonly System.Collections.Generic.Dictionary<StringName, AbilityData> Abilities = new();
    public readonly System.Collections.Generic.Dictionary<StringName, AbilityState> AbilityStates = new();

    public bool IsCasting { get; private set; }

    [Signal]
    public delegate void AbilityCastStartEventHandler(Unit unit, AbilityData ability);

    [Signal]
    public delegate void AbilityCastEndEventHandler(Unit unit, AbilityData ability);

    public override void _Ready()
    {
        _log = _log.ForContext("NameTag", Unit?.UnitName)
            .ForContext("IdTag", Unit?.UnitId);
        if (Unit is null) _log.Here().Warn("Unit for AbilityComponent is null, couldn't set Context");

        foreach (var poolData in InitialPoolDatas)
        {
            if (!Pools.TryAdd(poolData.Name, (PoolData)poolData.Duplicate()))
                throw new ArgumentException($"Pool '{poolData.Name}' with the same name is already registered");
        }

        InitialPoolDatas.Clear();
        
        foreach (var abilityData in InitialAbilityDatas)
        {
            if (!Abilities.TryAdd(abilityData.Name, abilityData))
                throw new ArgumentException(
                    $"An Ability '{abilityData.Name}' with the same name is already registered");
            if (!Pools.ContainsKey(abilityData.PoolName))
                throw new ArgumentException($"Pool '{abilityData.PoolName}' not found");
        }

        InitialAbilityDatas.Clear();

        ResetPool();
    }

    // -- Pool Management --

    public void ResetPool()
    {
        foreach (var (_, poolData) in Pools)
        {
            poolData.CurrentCount = poolData.MaxCount;
        }
    }
    
    public bool TryCanAffordBaseCost(StringName name, out bool canAfford)
    {
        canAfford = false;

        if (!Abilities.TryGetValue(name, out var ability)) return false;
        if (!Pools.TryGetValue(ability.PoolName, out var poolData)) return false;

        if (ability.BaseCost <= poolData.CurrentCount)
            canAfford = true;
        
        return true;
    }


    // -- Ability Management --
    public AbilityPayload CreatePayload(
        StringName abilityName,
        List<Vector2I> targetTiles,
        Board board
    )
    {
        if (!TryGetAbilityState(abilityName, out var state))
            throw new ArgumentException($"Ability '{abilityName}' not found");

        var payload = new AbilityPayload
        {
            ProcessingTiles = targetTiles,
            CurrentOrigin = Unit.TileLocation,
            State = state
        };
        return payload;
    }

    public bool TryGetAbilityState(StringName abilityName, [NotNullWhen(true)] out AbilityState? state)
    {
        state = null;
        if (!Abilities.TryGetValue(abilityName, out _))
            return false;

        if (!AbilityStates.ContainsKey(abilityName))
            AbilityStates[abilityName] = new AbilityState();
        state = AbilityStates[abilityName];
        return true;
    }

    // -- CASTING METHODS --

    /// <summary>
    /// Main validation dispatcher. Validates target count and reach once for the ability,
    /// then runs each effect's payload update (sequential or batch) in insertion order,
    /// and finally checks affordability against the fully updated payload (unless skipCostCheck is true).
    /// </summary>
    public Result<AbilityPayload, CastError> ValidateCast(
        StringName abilityName,
        AbilityContext context,
        List<Vector2I> targetedTiles,
        bool skipCostCheck = false)
    {
        if (!Abilities.TryGetValue(abilityName, out var ability))
        {
            return Result.Fail<AbilityPayload, CastError>(new CastError.AbilityNotFound(abilityName));
        }

        if (!TryGetAbilityState(abilityName, out var liveState))
        {
            return Result.Fail<AbilityPayload, CastError>(new CastError.AbilityNotFound(abilityName));
        }

        var validationResult = ability.ValidateCast(context, targetedTiles, state: liveState);
        if (validationResult.IsFailure)
            return validationResult;

        var payload = validationResult.Value;

        if (!skipCostCheck)
        {
            if (!Pools.TryGetValue(ability.PoolName, out var poolData))
            {
                return Result.Fail<AbilityPayload, CastError>(
                    new CastError.CannotAfford(ability.PoolName.ToString(), ability.GetCost(context, payload), 0));
            }

            var cost = ability.GetCost(context, payload);
            if (cost > poolData.CurrentCount)
            {
                return Result.Fail<AbilityPayload, CastError>(
                    new CastError.CannotAfford(ability.PoolName.ToString(), cost, poolData.CurrentCount));
            }
        }

        return Result.Ok<AbilityPayload, CastError>(payload);
    }

    public async Task<Result<CastOutcome, CastError>> CastAsync(
        StringName abilityName,
        AbilityContext context,
        List<Vector2I> targetedTiles)
    {
        if (!Abilities.TryGetValue(abilityName, out var ability))
            return Result.Fail<CastOutcome, CastError>(new CastError.AbilityNotFound(abilityName));

        var validationResult = ValidateCast(abilityName, context, targetedTiles, skipCostCheck: false);
        if (validationResult.IsFailure)
            return Result.Fail<CastOutcome, CastError>(validationResult.Error);

        var payload = validationResult.Value;
        var cost = ability.GetCost(context, payload);

        IsCasting = true;

        EmitSignal(SignalName.AbilityCastStart, Unit, ability);
        EventBus.Instance.EmitSignal(EventBus.SignalName.AbilityCastStart, Unit, ability);

        try
        {
            TryGetAbilityState(abilityName, out var abilityState);
            var castResult = await ability.CastAsync(context, targetedTiles, abilityState!);

            if (castResult.IsFailure)
            {
                _log.Here().Err("Ability '{AbilityName}' execution failed with {Error}",
                    ability.Name, castResult.Error);
                return Result.Fail<CastOutcome, CastError>(castResult.Error);
            }

            // Commit state changes atomically upon guaranteed success
            Pools[ability.PoolName].CurrentCount -= cost;
            EventBus.Instance.EmitSignal(EventBus.SignalName.AbilityCostUpdated, Unit, ability,
                Pools[ability.PoolName].CurrentCount);

            _log.Here().Info(
                "Casted ability '{AbilityName}' at {TargetCount} targets, cost: {Cost} from pool '{PoolName}'",
                ability.Name,
                targetedTiles.Count,
                cost,
                ability.PoolName);

            var outcome = new CastOutcome(
                castResult.Value.Outcomes, 
                cost, 
                ability.PoolName.ToString()
                );
            return Result.Ok<CastOutcome, CastError>(outcome);
        }
        finally
        {
            IsCasting = false;
            EmitSignal(SignalName.AbilityCastEnd, Unit, ability);
            EventBus.Instance.EmitSignal(EventBus.SignalName.AbilityCastEnd, Unit, ability);
        }
    }
}
