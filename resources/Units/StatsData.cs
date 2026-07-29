using Godot;
using System;
using AKidsDream.Globals;

namespace AKidsDream.Units;

[GlobalClass]
public partial class StatsData : Resource
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
