using Godot;
using System;
using AKidsDream.Components;
using AKidsDream.Globals;
using AKidsDream.Units;

public partial class DeathComponent : Node
{
	[Export] public Node Body;
	[Export] public StringName OnUnitKilledCallEventBus;
	[Export] public HealthComponent HealthC;
	[Signal] public delegate void UnitKilledEventHandler(Unit unit);

	public override void _Ready()
	{
		HealthC.HealthDepleted += OnHealthDepleted;
	}

	private void OnHealthDepleted()
	{
		EmitSignal(SignalName.UnitKilled, Body);
		if (!string.IsNullOrEmpty(OnUnitKilledCallEventBus))
			EventBus.Instance.EmitSignal(OnUnitKilledCallEventBus, Body);
		Body.QueueFree();
		GD.Print("Unit killed");
	}
}
