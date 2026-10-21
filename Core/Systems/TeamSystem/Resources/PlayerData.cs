using System.Collections.Generic;
using AKidsDream.Util.Identifiers;
using AKidsDream.Managers.SaveSystems;
using Godot;
using Godot.Collections;

namespace AKidsDream.Core.Teams;

[GlobalClass]
public partial class PlayerData : Resource
{
	[Export] public int PlayerIdInt;
	[Export] public string Name;
	[Export] public int TeamIdInt;
	[Export] public Global.UnitColor UnitColor;
	[Export] public string ControllerKey;
	[Export] public bool Defeated;
	[Export] public Array<string> PlayerHand = [];
	
	private int _mana;
	[Export]
	public int Mana
	{
		get => _mana;
		set => _mana = Mathf.Max(value, 0);
	}
	
	public PlayerId PlayerId => new(PlayerIdInt);
	public TeamId TeamId => new(TeamIdInt);
	public IController Controller { get; set; }
	
	public PlayerData() { }
	
	public PlayerData(
		PlayerId playerId, 
		string name, 
		TeamId teamId, 
		Global.UnitColor unitColor, 
		string controllerKey,
		int mana
		)
	{
		PlayerIdInt = playerId.Value;
		Name = name;
		TeamIdInt = teamId.Value;
		UnitColor = unitColor;
		ControllerKey = controllerKey;
		Mana = mana;
	}
	
	// ------------------------------------------------ Utils ------------------------------------------------
	public bool UsesPIC => ControllerKey == "PlayerInteractionController";
}
