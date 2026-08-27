#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using AKidsDream.Abilities;
using Godot;
using AKidsDream.Abilities.Effects;
using AKidsDream.GameBoard;
using AKidsDream.Managers.SaveSystems;
using AKidsDream.Common.Logging;
using Godot.Collections;
using Serilog;

namespace AKidsDream.Common.Components;

[GlobalClass]
[Icon("res://Assets/NodeIcons/star.svg")]
public partial class AbilityComponent : Node
{
	[Export] public Unit Unit = null!;

	private ILogger _log = GameLogger.For<AbilityComponent>();

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
	public delegate void AbilityCastStartEventHandler(Unit unit, StringName abilityName);

	[Signal]
	public delegate void AbilityCastEndEventHandler(Unit unit, AbilityData action, EffectResult result);

	public override void _Ready()
	{
		_log = _log.ForContext("UnitName", Unit?.UnitName)
			.ForContext("UnitId", Unit?.UnitId);
		if (Unit is null) _log.Here().Warn("Unit for AbilityComponent is null, couldn't set Context");
		
		foreach (var abilityData in InitialAbilityDatas)
		{
			if (!Abilities.TryAdd(abilityData.Name, abilityData))
				throw new ArgumentException(
					$"An Ability '{abilityData.Name}' with the same name is already registered");
		}
		InitialAbilityDatas.Clear();
		
		foreach (var poolData in InitialPoolDatas)
		{
			if (!Pools.TryAdd(poolData.Name, poolData))
				throw new ArgumentException($"Pool '{poolData.Name}' with the same name is already registered");
		}
		InitialPoolDatas.Clear();

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

	/// <summary>
	/// Checks if the unit has enough ability points to use the specified ability.
	/// </summary>
	/// <param name="name">The name of the ability to check.</param>
	/// <param name="context">The context for the cast.</param>
	/// <param name="payload">The payload for the cast.</param>
	/// <returns>True if the unit has enough ability points,
	/// False if the unit does not have enough ability points or the ability does not exist.</returns>
	public bool CanAfford(StringName name, AbilityContext context, AbilityPayload payload)
	{
		if (!Abilities.TryGetValue(name, out var ability)) return false;
		if (!Pools.TryGetValue(ability.PoolName, out var poolData)) return false;

		return ability.GetCost(context, payload) <= poolData.CurrentCount;
	}

	public bool TryCanAffordBaseCost(StringName name, out bool canAfford)
	{
		canAfford = false;

		if (!Abilities.TryGetValue(name, out var ability)) return false;
		if (!Pools.TryGetValue(ability.PoolName, out var poolData)) return false;

		if (ability.Cost > poolData.CurrentCount) return false;
		
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
			// AdditionalReachTiles = reachTiles,
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

	/// <summary>
	/// Ignores Cost checks. To do Cost checks use <see cref="CanAfford"></see>.
	/// Returns the valid tiles for the specified ability using execution.
	/// This allows effects to modify the reach via context state.
	/// </summary>
	/// <param name="name">The name of the <see cref="AbilityData"/></param>
	/// <param name="context">The context for the cast.</param>
	/// <param name="payload">The payload for the cast.</param>
	/// <returns>An array of valid tiles for the specified ability.</returns>
	public Vector2I[] ValidTiles(StringName name, AbilityContext context, AbilityPayload payload)
	{
		if (!Abilities.TryGetValue(name, out var ability)) return [];
		return ability.ReachPattern.GetTiles(payload.CurrentOrigin, context.Board, context.CasterId);
	}


	// -- CASTING METHODS --

	/// <summary>
	/// Main validation dispatcher. Validates target count and reach once for the ability,
	/// then runs each effect's payload update (sequential or batch) in insertion order,
	/// and finally checks affordability against the fully updated payload (unless skipCostCheck is true).
	/// </summary>
	public bool ValidateCast(
		StringName abilityName,
		AbilityContext context,
		List<Vector2I> targetedTiles,
		[NotNullWhen(true)] out AbilityPayload? payload,
		out CastFailureReason reason,
		bool skipCostCheck = false)
	{
		reason = CastFailureReason.None;
		payload = null;

		if (!Abilities.TryGetValue(abilityName, out var ability))
		{
			reason = CastFailureReason.AbilityNotFound;
			return false;
		}

		if (!TryGetAbilityState(abilityName, out var liveState))
		{
			reason = CastFailureReason.AbilityNotFound;
			return false;
		}

		if (!ability.HasValidTargetCount(targetedTiles))
		{
			reason = CastFailureReason.InvalidTargetsSelected;
			return false;
		}

		var state = liveState.Copy();

		payload = new AbilityPayload
		{
			CurrentOrigin = context.Caster.TileLocation,
			ProcessingTiles = targetedTiles,
			AccumulatedTargets = targetedTiles,
			State = state
		};

		for (var i = 0; i < ability.Effects.Length; i++)
		{
			var effect = ability.Effects[i];
			var isFirst = i == 0;

			if (effect.RunSequential)
			{
				// Sequential effect checks reachability for each tile itself
				if (!TryUpdatePayloadSequential(ability, effect, context, targetedTiles, payload, isFirst, out reason))
					return false;
			}
			else
			{
				// Batch effect checks reachability for the origin
				if (isFirst && !AllTilesInReach(ability, context, targetedTiles, payload.CurrentOrigin))
				{
					reason = CastFailureReason.TilesOutOfRange;
					return false;
				}

				UpdatePayloadBatch(effect, context, targetedTiles, payload);
			}
		}

		if (!skipCostCheck && !CanAfford(abilityName, context, payload))
		{
			reason = CastFailureReason.CannotAfford;
			return false;
		}

		return true;
	}

	/// <summary>
	/// Updates the payload for a single batch effect: all targeted tiles are
	/// processed together in one call, matching EffectData.Execute's non-sequential branch.
	/// </summary>
	private static void UpdatePayloadBatch(
		EffectData effect,
		AbilityContext context,
		List<Vector2I> targetedTiles,
		AbilityPayload payload)
	{
		payload.ProcessingTiles = targetedTiles;
		payload.AccumulatedTargets = targetedTiles;
		effect.UpdatePayload(context, payload);
	}

	private static bool AllTilesInReach(AbilityData ability, AbilityContext context, IEnumerable<Vector2I> tiles,
		Vector2I origin)
	{
		var validTiles = ability.ReachPattern.GetTiles(origin, context.Board, context.CasterId);
		return tiles.All(validTiles.Contains);
	}

	private static bool TryUpdatePayloadSequential(
		AbilityData ability,
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
			if (checkReach && !AllTilesInReach(ability, context, [tile], payload.CurrentOrigin))
			{
				reason = CastFailureReason.TilesOutOfRange;
				return false;
			}

			payload.AccumulatedTargets.Add(tile);
			payload.ProcessingTiles = [tile];
			effect.UpdatePayload(context, payload); // e.g. teleport moves CurrentOrigin here
		}

		return true;
	}

	public async Task<CastResult> CastAsync(StringName abilityName, AbilityContext context,
		List<Vector2I> targetedTiles)
	{
		if (!Abilities.TryGetValue(abilityName, out var ability))
			return CastResult.Fail(CastFailureReason.AbilityNotFound);

		if (!ValidateCast(abilityName, context, targetedTiles, out var payload, out var reason))
			return CastResult.Fail(reason);

		IsCasting = true;

		EmitSignal(SignalName.AbilityCastStart, Unit, ability);
		EventBus.Instance.EmitSignal(EventBus.SignalName.AbilityCastStart, Unit, ability);
		
		EffectResult effectResult = new CompositeResult();

		try
		{
			Pools[ability.PoolName].CurrentCount -= ability.GetCost(context, payload);
			EventBus.Instance.EmitSignal(EventBus.SignalName.AbilityCostUpdated, Unit, ability, Pools[ability.PoolName].CurrentCount);
			
			TryGetAbilityState(abilityName, out var abilityState);
			(effectResult, var _) = await ability.CastAsync(context, targetedTiles, abilityState!);
			
			if (effectResult is ErrorResult errorResult)
			{
				_log.Here().Err("Ability '{AbilityName}' execution with effect: {EffectType} failed with {Error}",
					ability.Name, errorResult.Error, errorResult.Effect.GetType().Name);
				return CastResult.Fail(CastFailureReason.EffectExecutionFailed, effectResult);
			}

			_log.Here().Info(
				"Casted ability '{AbilityName}' at {TargetCount} targets, cost: {Cost} from pool '{PoolName}'",
				ability.Name,
				targetedTiles.Count,
				ability.Cost,
				ability.PoolName);

			return CastResult.Ok(effectResult);
		}
		finally
		{
			IsCasting = false;
			EmitSignal(SignalName.AbilityCastEnd, Unit, ability, effectResult);
			EventBus.Instance.EmitSignal(EventBus.SignalName.AbilityCastEnd, Unit, ability, effectResult);
		}
	}
}
