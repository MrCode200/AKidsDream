using Godot;
using System;
using AKidsDream.Abilities;
using AKidsDream.Components;
using AKidsDream.Globals;
using AKidsDream.Units;

namespace AKidsDream.UI;

public partial class UnitInfoBar : Control
{
	[Export] public Label UnitNameLabel;
	[Export] public Label UnitHealthLabel;
	[Export] public HBoxContainer AbilityContainer;
	[Export] public PackedScene AbilityBtnScene;

	public override void _Ready()
	{
		EventBus.Instance.UnitSelected += CreateUnitBar;
		EventBus.Instance.UnitDeselected += (Unit unit) => Visible = false;
	}
	
	public void CreateUnitBar(Unit unit)
	{
		Visible = true;
		
		foreach (Node child in AbilityContainer.GetChildren()) child.QueueFree();
		
		UnitNameLabel.Text = unit.UnitName.GetFieldValue<string>();
		UnitHealthLabel.Text = unit.Stats.Health.ToString();
		
		AbilityComponent abilityC = unit.AbilityC;
		foreach (AbilityData ability in abilityC.Abilities)
		{
			var newAbilityBtn = AbilityBtnScene.Instantiate<AbilityBtn>();
			newAbilityBtn.DisplayAbility(unit, ability);
			AbilityContainer.AddChild(newAbilityBtn);
		}
	}
}
