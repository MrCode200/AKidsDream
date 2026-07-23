using AKidsDream.Units;
using Godot;

namespace AKidsDream.Scripts;

public partial class EventBus : Node
{
	public static EventBus Instance { get; private set; }
	
	[Signal] public delegate void UnitSelectedEventHandler(Unit unit);
	[Signal] public delegate void UnitDeselectedEventHandler(Unit unit);
	[Signal] public delegate void UnitCreatedEventHandler(Unit unit);
	[Signal] public delegate void UnitKilledEventHandler(Unit unit);
	[Signal] public delegate void UnitMovedEventHandler(Unit unit, Vector2I oldTile, Vector2I newTile);

	public override void _Ready()
	{
		Instance = this;
	}
}
