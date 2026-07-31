using Godot;
using AKidsDream.Managers.SaveSystems;

namespace AKidsDream.Units.Resources.Components;

[GlobalClass]
[Icon("res://Assets/Node Icons/icon-skull-50.png")]
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
