using AKidsDream.Abilities;
using AKidsDream.Abilities.Effects;
using AKidsDream.Units.Resources;
using Godot;

namespace AKidsDream.Managers.SaveSystems;

public partial class EventBus : Node
{
	public static EventBus Instance { get; private set; }
	
	// -- GAME SIGNALS --
	[Signal] public delegate void NewTurnStartedEventHandler(int newTurn);
	[Signal] public delegate void PlayerTurnEndedEventHandler();
	[Signal] public delegate void EnemyTurnEndedEventHandler();
	
	// -- BOARD SIGNALS --
	[Signal] public delegate void BoardGeneratedEventHandler();
	
	// -- UNIT SIGNALS --
	// NOTE: Unit Created gets emitted on _Ready not on Unit.Init(...);
	[Signal] public delegate void UnitCreatedEventHandler(Unit unit);
	[Signal] public delegate void UnitSelectedEventHandler(Unit unit);
	[Signal] public delegate void UnitDeselectedEventHandler(Unit unit);
	[Signal] public delegate void UnitKilledEventHandler(Unit unit);
	[Signal] public delegate void UnitMovedEventHandler(Unit unit, Vector2I oldTile, Vector2I newTile);
	
	// -- Abilities --
	[Signal] public delegate void AbilityCastEventHandler(Unit unit, AbilityData ability, EffectResult result);
	
	// -- UI SIGNALS --
	[Signal] public delegate void AbilityBtnPressedEventHandler(Unit unit, AbilityData ability);
	[Signal] public delegate void EndTurnBtnPressedEventHandler();

	public override void _Ready()
	{
		Instance = this;
	}
}
