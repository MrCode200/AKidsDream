using Godot;
using System;
using AKidsDream.Managers.SaveSystems;

namespace AKidsDream.Common;

[GlobalClass]
[Tool]
public partial class UnitStatsData : Resource
{
	[Export] public Global.UnitName UnitName;
	
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
