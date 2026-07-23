using System;
using Godot;
using AKidsDream.Units;

namespace AKidsDream.Components;

/// <summary>
/// Manages health-related functionality for a game entity.
/// Handles damage, healing, and death events through signals.
/// </summary>
[GlobalClass]
public partial class HealthComponent : Node
{
	/// <summary>
	/// The stats data containing health information for this component.
	/// </summary>
	public StatsData Stats;
	
	/// <summary>
	/// Emitted when the health value changes.
	/// Provides the amount of change (positive for healing, negative for damage).
	/// </summary>
	[Signal] public delegate void HealthChangedEventHandler(int amount);
	
	/// <summary>
	/// Emitted when the entity is killed (health reaches 0 or below).
	/// </summary>
	[Signal] public delegate void HealthDepletedEventHandler();
	
	/// <summary>
	/// Sets the maximum health value.
	/// If the current health exceeds the new maximum, it will be capped to the new max value.
	/// Note: This method does not emit a HealthChanged signal when capping occurs.
	/// </summary>
	/// <param name="amount">The new maximum health value.</param>
	public void SetMaxHealth(int amount)
	{
		Stats.MaxHealth = amount;
		if (Stats.Health > amount)
		{
			Stats.Health = amount;
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
		Stats.Health -= amount;
		EmitSignal(SignalName.HealthChanged, amount);
		if (Stats.Health <= 0)
		{
			OnHealthDepleted();
		}
	}
	
	/// <summary>
	/// Heals the entity by increasing its health.
	/// Emits HealthChanged signal with the heal amount.
	/// </summary>
	/// <param name="amount">The amount of health to restore.</param>
	public void Heal(int amount)
	{
		amount = Math.Min(Stats.MaxHealth - Stats.Health, amount);
		Stats.Health += amount;
		
		EmitSignal(SignalName.HealthChanged, amount);
	}
	
	/// <summary>
	/// Kills the entity by emitting the Killed signal.
	/// </summary>
	public void OnHealthDepleted()
	{
		EmitSignal(SignalName.HealthDepleted);
	}
}
