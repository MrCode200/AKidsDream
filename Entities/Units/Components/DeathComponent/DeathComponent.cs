using Godot;
using AKidsDream.Managers.SaveSystems;
using AKidsDream.Common.Logging;
using Serilog;

namespace AKidsDream.Units.Resources.Components;

[GlobalClass]
[Icon("res://Assets/Node Icons/icon-skull-50.png")]
public partial class DeathComponent : Node
{
	[Export] public Unit Unit;
	[Export] public StringName OnUnitKilledCallEventBus;
	[Export] public HealthComponent HealthC;
	[Signal] public delegate void UnitKilledEventHandler(Unit unit);

	private ILogger _log = GameLogger.For<DeathComponent>();

	public override void _Ready()
	{
		_log = _log.ForContext("UnitName", Unit.UnitName)
			.ForContext("UnitId", Unit.UnitId);
		HealthC.HealthDepleted += OnHealthDepleted;
	}

	public override void _ExitTree()
	{
		HealthC.HealthDepleted -= OnHealthDepleted;
	}

	private void OnHealthDepleted()
	{
		_log.Here().Info("Unit killed");

		EmitSignal(SignalName.UnitKilled, Unit);
		if (!string.IsNullOrEmpty(OnUnitKilledCallEventBus))
			EventBus.Instance.EmitSignal(OnUnitKilledCallEventBus, Unit);
		Unit.QueueFree();
	}
}
