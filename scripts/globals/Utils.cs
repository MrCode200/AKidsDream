using System;
using AKidsDream.GameBoard;
using Godot;

namespace AKidsDream.Scripts;

public partial class Utils : Node
{
	public static Utils Instance { get; private set; }
	
	public enum UnitTeam
	{
		Player,
		Enemy
	}

	public override void _Ready()
	{
		Instance = this;
	}
}
