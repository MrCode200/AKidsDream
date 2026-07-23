using Godot;
using System;
using AKidsDream.Scripts;

namespace AKidsDream.Units;

[GlobalClass]
public partial class StatsData : Resource
{
	private static int _nextId = 0;

	[Export] public int UnitId { get; private set; }
	[Export] public Utils.UnitTeam Team;
	
	[Export] public int MaxHealth;
	[Export] public int Health;
	[Export] public int Attack;
	
	public StatsData()
	{
		UnitId = _nextId++;
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
