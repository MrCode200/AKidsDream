using Godot;
using System;
using AKidsDream.Globals;

namespace AKidsDream.Units;

[GlobalClass]
[Tool]
public partial class UnitStatsData : Resource
{
	[Export] public int MaxHealth;
	[Export] public int Health;
	/*
	public StatsData()
	{
		if (Health == 0)  // Or use default(float) which is 0
		{
			Health = MaxHealth;
		}
	}
	*/
}
