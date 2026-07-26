using Godot;
using System;
using AKidsDream.Globals;

namespace AKidsDream.Units;

[GlobalClass]
public partial class StatsData : Resource
{
	public int UnitId { get; private set; }
	[Export] public Global.UnitName UnitName;
	[Export] public Global.UnitTeam Team;
	
	[Export] public int MaxHealth;
	[Export] public int Health;
	[Export] public int Attack;
	
	public StatsData()
	{
		UnitId = Utils.GetNextId();
	}

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
