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

[GlobalClass]
[Icon("res://Assets/Node Icons/icon-attack-50.png")]
public partial class AbilityComponent : Node
{
	[Export] public Unit Unit;

	private ILogger _log = GameLogger.For<AbilityComponent>();

	/// <summary>
	/// Contains the current ability points for each pool.
	/// Where the key is the pool name and the value is the max ability points per turn.
	/// </summary>
	[Export] public Dictionary<StringName, int> MaxAbilityPoints = new()
	{
		{ "Move", 1 },
		{ "Combat", 1 },
	};
	
	[Export] public Array<AbilityData> Abilities = new();


	public Dictionary<StringName, int> RemainingAbilityPoints;

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

	public AbilityData GetAbility(StringName name) => Abilities.FirstOrDefault(a => a.Name == name);

	/// <summary>
	/// Checks if the unit has enough ability points to use the specified ability.
	/// </summary>
	/// <param name="name">The name of the ability to check.</param>
	/// <returns>True if the unit has enough ability points,
	/// False if the unit does not have enough ability points or the ability does not exist.</returns>
	public bool CanAfford(StringName name)
	{
		AbilityData ability = GetAbility(name);
		
		if (ability == null) return false;
		if (!RemainingAbilityPoints.ContainsKey(ability.PoolName)) return false;

		return ability.Cost <= RemainingAbilityPoints[ability.PoolName];
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
		return action.ReachPattern.GetTiles(Unit.TileLocation, board);
	}

	/// <summary>
	/// Casts the specified <see cref="AbilityData"/>.Effect(<see cref="EffectData"/>) on the targeted tiles.
	/// To get the <see cref="EffectResult"/>. Subscribe to the <see cref="AbilityAppliedEventHandler"/> signal.
	/// </summary>
	/// <param name="name">The name of the <see cref="AbilityData"/></param>
	/// <param name="targetTiles">Onto which tiles the <see cref="AbilityData"/>.Effect(<see cref="EffectData"/>) should be applied</param>
	/// <param name="board">The board instance, passed as dependency for other functions.</param>
	/// <returns>True if the ability was cast, False if the ability could not be cast on the selected tiles.
	/// <list type="number">
	/// <item>True if the ability was cast</item>
	/// <item>False if the ability could not be afforded</item>
	/// <item>False if the ability wasn't registered</item>
	/// <item>False if the tiles aren't in the reach pattern</item>
	/// <item>False if not enough Points are in the Pool</item>
	/// </list></returns>
	public bool Cast(StringName name, Vector2I[] targetTiles, Board board)
	{
		var ability = GetAbility(name);
		if (ability is null)
		{
			_log.Here().Warn("Ability '{AbilityName}' not found", name);
			return false;
		}
		if (!CanAfford(name))
		{
			_log.Here().Debug("Cannot cast ability '{AbilityName}': cannot afford", ability.Name);
			return false;
		}
		
		if (targetTiles.Any(tile => !ValidTiles(name, board).Contains(tile)))
		{
			_log.Here().Debug("Cannot cast ability '{AbilityName}': tile not in reach", ability.Name);
			return false;
		}

		var result = ability.Effect.Apply(Unit, board, targetTiles);

		if (result is ErrorResult errorResult)
		{
			_log.Here().Error("Ability '{AbilityName}' execution with effect: {EffectType} failed with {Error}", 
				ability.Name, errorResult.Error, errorResult.Effect.GetType().Name);
		}
		else
		{
			_log.Here().Info(
				"Casted ability '{AbilityName}' at {TargetCount} targets, cost: {Cost} from pool '{PoolName}'",
				ability.Name,
				targetTiles.Length,
				ability.Cost,
				ability.PoolName);
			RemainingAbilityPoints[ability.PoolName] -= ability.Cost;
		}


		EmitSignal(SignalName.AbilityCast, Unit, ability, result);
		EventBus.Instance.EmitSignal(EventBus.SignalName.AbilityCast, Unit, ability, result);
		return true;
	}
}
