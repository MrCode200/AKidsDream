#nullable enable
using System.Linq;
using AKidsDream.Abilities;
using Godot;
using AKidsDream.Abilities.Effects;
using AKidsDream.GameBoard;
using AKidsDream.Managers.SaveSystems;
using AKidsDream.Common.Logging;
using Godot.Collections;
using Serilog;
using Array = System.Array;

namespace AKidsDream.Units.Resources.Components;

public enum CastFailureReason
{
	None,
	AbilityNotFound,
	CannotAfford,
	TilesOutOfRange,
	EffectExecutionFailed
}

public readonly record struct CastResult(bool Success, CastFailureReason FailureReason, EffectResult? EffectResult)
{
	public static CastResult Ok(EffectResult effectResult) => new(true, CastFailureReason.None, effectResult);
	public static CastResult Fail(CastFailureReason reason, EffectResult? effectResult = null) => new(false, reason, effectResult);
}

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
	[Export] public Dictionary<StringName, int> MaxAbilityPoints = new()
	{
		{ "Move", 1 },
		{ "Combat", 1 }
	};
	
	[Export] public Array<AbilityData> Abilities = [];


	public Dictionary<StringName, int> RemainingAbilityPoints = new();

	[Signal]
	public delegate void AbilityCastEventHandler(Unit unit, AbilityData action, EffectResult result);

	public override void _Ready()
	{
		_log = _log.ForContext("UnitName", Unit?.UnitName)
			.ForContext("UnitId", Unit?.UnitId);
		if (Unit is null) _log.Here().Warn("Unit for AbilityComponent is null, couldn't set Context");
		ResetPool();
	}

	public void ResetPool()
	{
		RemainingAbilityPoints = MaxAbilityPoints.Duplicate(true);
	}

	public AbilityData? GetAbility(StringName name) => Abilities.FirstOrDefault(a => a.Name == name);

	/// <summary>
	/// Checks if the unit has enough ability points to use the specified ability.
	/// </summary>
	/// <param name="name">The name of the ability to check.</param>
	/// <returns>True if the unit has enough ability points,
	/// False if the unit does not have enough ability points or the ability does not exist.</returns>
	public bool CanAfford(StringName name)
	{
		AbilityData? ability = GetAbility(name);
		
		if (ability == null) return false;
		if (!RemainingAbilityPoints.TryGetValue(ability.PoolName, out var point)) return false;

		return ability.Cost <= point;
	}

	/// <summary>
	/// Returns the valid tiles for the specified ability.
	/// Ignores Cost checks. To do Cost checks use <see cref="CanAfford(StringName)"></see>.
	/// </summary>
	/// <param name="name">The name of the <see cref="AbilityData"/>.</param>
	/// <param name="board">The board instance</param>
	/// <returns></returns>
	public Vector2I[] ValidTiles(StringName name, Board board)
	{
		var action = GetAbility(name);
		if (action?.ReachPattern is null) return Array.Empty<Vector2I>();
		return action.ReachPattern.GetTiles(Unit.TileLocation, board, Unit.OwnerId);
	}

	/// <summary>
	/// Casts the specified <see cref="AbilityData"/>.Effect(<see cref="EffectData"/>) on the targeted tiles.
	/// To get the <see cref="EffectResult"/>. Subscribe to the <see cref="AbilityCast"/> signal.
	/// </summary>
	/// <param name="name">The name of the <see cref="AbilityData"/></param>
	/// <param name="targetTiles">Onto which tiles the <see cref="AbilityData"/>.Effect(<see cref="EffectData"/>) should be applied</param>
	/// <param name="board">The board instance, passed as dependency for other functions.</param>
	/// <returns>CastResult indicating success or failure with specific reason.</returns>
	public CastResult Cast(StringName name, Vector2I[] targetTiles, Board board)
	{
		var ability = GetAbility(name);
		if (ability is null)
		{
			_log.Here().Warn("Ability '{AbilityName}' not found", name);
			return CastResult.Fail(CastFailureReason.AbilityNotFound);
		}
		if (!CanAfford(name))
		{
			_log.Here().Debug("Cannot cast ability '{AbilityName}': cannot afford", ability.Name);
			return CastResult.Fail(CastFailureReason.CannotAfford);
		}
		
		if (targetTiles.Any(tile => !ValidTiles(name, board).Contains(tile)))
		{
			_log.Here().Debug("Cannot cast ability '{AbilityName}': tile not in reach", ability.Name);
			return CastResult.Fail(CastFailureReason.TilesOutOfRange);
		}

		var effectResult = ability.Effect.Apply(Unit, board, targetTiles);

		if (effectResult is ErrorResult errorResult)
		{
			_log.Here().Error("Ability '{AbilityName}' execution with effect: {EffectType} failed with {Error}", 
				ability.Name, errorResult.Error, errorResult.Effect.GetType().Name);
			return CastResult.Fail(CastFailureReason.EffectExecutionFailed, effectResult);
		}
		
		_log.Here().Info(
			"Casted ability '{AbilityName}' at {TargetCount} targets, cost: {Cost} from pool '{PoolName}'",
			ability.Name,
			targetTiles.Length,
			ability.Cost,
			ability.PoolName);
		RemainingAbilityPoints[ability.PoolName] -= ability.Cost;

		EmitSignal(SignalName.AbilityCast, Unit, ability, effectResult);
		EventBus.Instance.EmitSignal(EventBus.SignalName.AbilityCast, Unit, ability, effectResult);
		return CastResult.Ok(effectResult);
	}
}
