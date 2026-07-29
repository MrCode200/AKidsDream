using AKidsDream.Abilities;
using AKidsDream.Units;
using Godot;

namespace AKidsDream.Globals;

public partial class EventBus : Node
{
	public static EventBus Instance { get; private set; }
	// -- BOARD SIGNALS --
	[Signal] public delegate void BoardGeneratedEventHandler();
	
	
	// -- UNIT SIGNALS --
	[Signal] public delegate void UnitSelectedEventHandler(Unit unit);
	[Signal] public delegate void UnitDeselectedEventHandler(Unit unit);
	[Signal] public delegate void UnitCreatedEventHandler(Unit unit);
	[Signal] public delegate void UnitKilledEventHandler(Unit unit);
	[Signal] public delegate void UnitMovedEventHandler(Unit unit, Vector2I oldTile, Vector2I newTile);
	
	// -- UI SIGNALS --
	[Signal] public delegate void AbilityBtnPressedEventHandler(Unit unit, AbilityData ability); //CHECK: Is Unit needed?

	public override void _Ready()
	{
		Instance = this;
	}
}
