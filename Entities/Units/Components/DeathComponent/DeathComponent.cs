using AKidsDream.Utilities;
using Godot;
using AKidsDream.Managers.SaveSystems;
using AKidsDream.Utilities.Logging;
using Serilog;

namespace TweenComp.Resources.Blueprint;

[GlobalClass]
[Icon("res://Entities/Units/Components/DeathComponent/Skull.png")]
public partial class DeathComponent : Node
{
	[Export] public Unit Unit;
	[Export] public StringName OnUnitKilledCallEventBus;
	[Export] public HealthComponent HealthC;
	[Signal] public delegate void UnitKilledEventHandler(Unit unit);

	private ILogger _log = GameLogger.For<DeathComponent>();

	public override void _Ready()
	{
		_log = _log.ForContext("UnitNameTag", Unit.UnitType)
			.ForContext("UnitIdTag", Unit.UnitId);
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
