#nullable enable
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

namespace AKidsDream.Units.Resources.Components;

[GlobalClass]
[Icon("res://Assets/Node Icons/icon-attack-50.png")]
public partial class AbilityComponent : Node
{
	[Export] public Unit Unit = null!;

	private ILogger _log = GameLogger.For<AbilityComponent>();

	/// <summary>
	/// Contains the current ability points for each pool.
	/// Where the key is the pool name and the value is the max ability points per turn.
	/// </summary>
	[Export] public Godot.Collections.Dictionary<StringName, int> MaxAbilityPoints = new()
	{
		{ "Move", 1 },
		{ "Combat", 1 }
	};

	[Export] public Array<AbilityData> AbilityDatas = [];
	public readonly System.Collections.Generic.Dictionary<StringName, AbilityData> Abilities = new();
	public readonly System.Collections.Generic.Dictionary<StringName, AbilityState> AbilityStates = new();
	
	public Godot.Collections.Dictionary<StringName, int> RemainingAbilityPoints = new();

	[Signal]
	public delegate void AbilityCastEventHandler(Unit unit, AbilityData action, EffectResult result);

	public override void _Ready()
	{
		_log = _log.ForContext("UnitName", Unit?.UnitName)
			.ForContext("UnitId", Unit?.UnitId);
		if (Unit is null) _log.Here().Warn("Unit for AbilityComponent is null, couldn't set Context");
		foreach (var abilityData in AbilityDatas)
		{
			Abilities[abilityData.Name] = abilityData;
		}

		ResetPool();
	}

	// -- Pool Management --

	public void ResetPool()
	{
		RemainingAbilityPoints = MaxAbilityPoints.Duplicate(true);
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
		if (!RemainingAbilityPoints.TryGetValue(ability.PoolName, out var point)) return false;

		return ability.GetCost(context, payload) <= point;
	}

	public bool CanAffordBaseCost(StringName name)
	{
		if (!Abilities.TryGetValue(name, out var ability)) return false;
		if (!RemainingAbilityPoints.TryGetValue(ability.PoolName, out var point)) return false;

		return ability.Cost <= point;
	}

	// -- Ability Management --
	public AbilityPayload CreatePayload(
		StringName abilityName,
		List<Vector2I> targetTiles,
		Board board
	)
	{
		if (!TryGetAbilityState(abilityName, out var state))
			throw new System.ArgumentException($"Ability '{abilityName}' not found");

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
	/// and finally checks affordability against the fully updated payload.
	/// </summary>
	public bool ValidateCast(
		StringName abilityName,
		AbilityContext context,
		List<Vector2I> targetedTiles,
		[NotNullWhen(true)] out AbilityPayload? payload,
		out CastFailureReason reason)
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
			reason = CastFailureReason.InvalidTargetCount;
			return false;
		}

		var state = liveState.Copy();

		payload = new AbilityPayload
		{
			CurrentOrigin = context.Source.TileLocation,
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
				// CHECK: if to move check into UpdatePayloadBatch?
				// Batch effect checks reachability for the origin
				if (isFirst && !AllTilesInReach(ability, context, targetedTiles, payload.CurrentOrigin))
				{
					reason = CastFailureReason.TilesOutOfRange;
					return false;
				}
				UpdatePayloadBatch(effect, context, targetedTiles, payload);
			}
		}

		if (!CanAfford(abilityName, context, payload))
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
	
	private static bool AllTilesInReach(AbilityData ability, AbilityContext context, IEnumerable<Vector2I> tiles, Vector2I origin)
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

	public async Task<CastResult> Cast(StringName abilityName, AbilityContext context, List<Vector2I> targetedTiles)
	{
		if (!Abilities.TryGetValue(abilityName, out var ability))
			return CastResult.Fail(CastFailureReason.AbilityNotFound);

		if (!ValidateCast(abilityName, context, targetedTiles, out _, out var reason))
			return CastResult.Fail(reason);

		// TODO:
		// change cost check in OnAbilitySelect, as that should only happen in the last sequential order (if cost gets cheaper let user still select tiles)
		// How to handle the logic for buttons disabling? (how to know when it should disable the button?, extra method?)
		// TODO: Make OnAbilitySelectedState handle failure of cast (what to do with tiles...) (stay selected, later add back key/btn to ability tiles)
		// CHECK:
		// if CastAbility in OnAbilitySelected is the only thing that needs async, should there be a check if task complete then allow other interaction?(play another ability)
		// Yes so no ability race condition!
		

		// CHeck if commands DeselectAbilityCOmmand and SelectAbilityCommand are needed? (as can be bug as DeselectUnitCommand takes Unit)

		// When finished ask devin to show how context gets handled
		// (ctx for interaction validation
		// should be separate than context for cast or other context)

		TryGetAbilityState(abilityName, out var abilityState);
		var (effectResult, payload) = await ability.Cast(context, targetedTiles, abilityState!);

		if (effectResult is ErrorResult errorResult)
		{
			_log.Here().Error("Ability '{AbilityName}' execution with effect: {EffectType} failed with {Error}",
				ability.Name, errorResult.Error, errorResult.Effect.GetType().Name);
			return CastResult.Fail(CastFailureReason.EffectExecutionFailed, effectResult);
		}

		_log.Here().Info(
			"Casted ability '{AbilityName}' at {TargetCount} targets, cost: {Cost} from pool '{PoolName}'",
			ability.Name,
			targetedTiles.Count,
			ability.Cost,
			ability.PoolName);
		RemainingAbilityPoints[ability.PoolName] -= ability.GetCost(context, payload);

		EmitSignal(SignalName.AbilityCast, Unit, ability, effectResult);
		EventBus.Instance.EmitSignal(EventBus.SignalName.AbilityCast, Unit, ability, effectResult);
		return CastResult.Ok(effectResult);
	}
}
