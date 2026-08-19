using AKidsDream.Abilities;
using AKidsDream.Abilities.Effects;
using AKidsDream.Units.Resources;
using Godot;

namespace AKidsDream.Managers.SaveSystems;

public partial class EventBus : Node
{
	public static EventBus Instance { get; private set; }

	public override void _EnterTree()
	{
		Instance = this;
	}
	
	public override void _ExitTree()
	{
		Instance = null;
	}

	// -- GAME SIGNALS --

	[Signal] public delegate void GameInitializedEventHandler();

	[Signal] public delegate void NewRoundStartedEventHandler(int playerIdInt, int newRound);

	[Signal] public delegate void TurnStartedEventHandler(int playerIdInt, int round);

	[Signal] public delegate void TurnEndedEventHandler(int playerIdInt, int round);

	// [Signal] public delegate void ActivePlayerChangedEventHandler(int previousPlayerIdInt, int currentPlayerIdInt, int round);

	[Signal] public delegate void RoundStartedEventHandler(int round);

	// -- UNIT SIGNALS --

	// NOTE: Unit Created gets emitted on _Ready not on Unit.Init(...);

	[Signal] public delegate void UnitCreatedEventHandler(Unit unit);

	[Signal] public delegate void UnitSelectedEventHandler(Unit unit);

	[Signal] public delegate void UnitDeselectedEventHandler(Unit unit);

	[Signal] public delegate void UnitKilledEventHandler(Unit unit);

	[Signal] public delegate void UnitMovedEventHandler(Unit unit, Vector2I oldTile, Vector2I newTile);

	// -- Abilities --

	[Signal] public delegate void AbilityCastStartEventHandler(Unit unit, AbilityData abilityData);
	[Signal] public delegate void AbilityCastEndEventHandler(Unit unit, AbilityData ability, EffectResult result);

	[Signal] public delegate void AbilityCostUpdatedEventHandler(Unit unit, AbilityData ability, int newCount);
	
	[Signal] public delegate void EffectTriggerStartEventHandler(Unit unit, AbilityData ability, EffectData effect);
	[Signal] public delegate void EffectTriggerEndEventHandler(Unit unit, AbilityData ability, EffectData effect);

	[Signal] public delegate void EffectApplyStartEventHandler(Unit unit, AbilityData ability, EffectData effect);
	[Signal] public delegate void EffectApplyEndEventHandler(Unit unit, AbilityData ability, EffectData effect, EffectResult result);

	
	
	// -- UI SIGNALS --

	[Signal] public delegate void AbilityBtnPressedEventHandler(Unit unit, AbilityData ability);

	[Signal] public delegate void EndTurnButtonPressedEventHandler(int callerPlayerIdInt);
}
