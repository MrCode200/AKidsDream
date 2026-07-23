using Godot;
using System;
using AKidsDream.Components;
using AKidsDream.Scripts;
using AKidsDream.Units;

public partial class DeathComponent : Node
{
	[Signal] public delegate void UnitKilledEventHandler(Unit unit);
	[Export] public Node Body;
	[Export] public StringName OnUnitKilledCallEventBus;
	[Export] public HealthComponent HealthC;
	
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
