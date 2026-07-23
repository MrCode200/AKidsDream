using Godot;
using System;
using AKidsDream.Components;
using AKidsDream.Units;

public partial class AttackComponent : Node
{
	public StatsData Stats;
	
	public void Attack(Unit target)
	{
		GD.Print($"'{Stats.UnitId}' Attacks '{target.Stats.UnitId}' with '{Stats.Attack}'dmg");
		target.GetNode<HealthComponent>("HealthComponent")?.Damage(Stats.Attack);    
	}
}
