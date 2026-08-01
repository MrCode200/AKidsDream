using System;
using Godot;
using AKidsDream.Common.Logging;
using Serilog;

namespace AKidsDream.Units.Resources.Components;

/// <summary>
/// Manages health-related functionality for a game entity.
/// Handles damage, healing, and death events through signals.
/// </summary>
[GlobalClass]
[Icon("res://Assets/Node Icons/icon-heart-50.png")]
public partial class HealthComponent : Node
{
	/// <summary>
	/// The stats data containing health information for this component.
	/// </summary>
	public UnitStatsData UnitStats;

	private ILogger _log = GameLogger.For<HealthComponent>();
	
	/// <summary>
	/// Emitted when the health value changes.
	/// Provides the amount of change (positive for healing, negative for damage).
	/// </summary>
	[Signal] public delegate void HealthChangedEventHandler(int amount);
	
	/// <summary>
	/// Emitted when the entity is killed (health reaches 0 or below).
	/// </summary>
	[Signal] public delegate void HealthDepletedEventHandler();

	public override void _Ready()
	{
		var unit = Owner as Unit;
		_log = _log.ForContext("UnitName", unit?.UnitName)
			.ForContext("UnitId", unit?.UnitId)
			.ForContext("Team", unit?.Team);
	}
	
	/// <summary>
	/// Sets the maximum health value.
	/// If the current health exceeds the new maximum, it will be capped to the new max value.
	/// Note: This method does not emit a HealthChanged signal when capping occurs.
	/// </summary>
	/// <param name="amount">The new maximum health value.</param>
	public void SetMaxHealth(int amount)
	{
		UnitStats.MaxHealth = amount;
		if (UnitStats.Health > amount)
		{
			UnitStats.Health = amount;
			_log.Here().Debug(
				"Max health set to {MaxHealth}, current health capped to {CurrentHealth}",
				amount,
				UnitStats.Health);
		}
		else
		{
			_log.Here().Debug("Max health set to {MaxHealth}", amount);
		}
	}
	
	/// <summary>
	/// Applies damage to the entity by reducing its health.
	/// Emits HealthChanged signal with the damage amount.
	/// If health reaches 0 or below, the entity is killed.
	/// </summary>
	/// <param name="amount">The amount of damage to apply.</param>
	public void Damage(int amount)
	{
		var previousHealth = UnitStats.Health;
		UnitStats.Health -= amount;
		EmitSignal(SignalName.HealthChanged, amount);

		_log.Here().Info(
			"Took {DamageAmount} damage, health: {PreviousHealth} → {CurrentHealth}/{MaxHealth}",
			amount,
			previousHealth,
			UnitStats.Health,
			UnitStats.MaxHealth);

		if (UnitStats.Health <= 0)
		{
			EmitSignal(SignalName.HealthDepleted);
		}
	}
	
	/// <summary>
	/// Heals the entity by increasing its health.
	/// Emits HealthChanged signal with the heal amount.
	/// </summary>
	/// <param name="amount">The amount of health to restore.</param>
	public void Heal(int amount)
	{
		var previousHealth = UnitStats.Health;
		amount = Math.Min(UnitStats.MaxHealth - UnitStats.Health, amount);
		UnitStats.Health += amount;
		
		if (amount > 0)
		{
			_log.Here().Info(
				"Healed {HealAmount} health, health: {PreviousHealth} → {CurrentHealth}/{MaxHealth}",
				amount,
				previousHealth,
				UnitStats.Health,
				UnitStats.MaxHealth);
		}

		EmitSignal(SignalName.HealthChanged, amount);
	}
}
