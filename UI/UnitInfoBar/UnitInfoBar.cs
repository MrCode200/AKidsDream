using Godot;
using System;
using AKidsDream.Abilities;
using AKidsDream.Abilities.Effects;
using AKidsDream.Units.Resources.Components;
using AKidsDream.Managers.SaveSystems;
using AKidsDream.Units.Resources;
using Godot.Collections;

namespace AKidsDream.UnitInfoBar.UI;

public partial class UnitInfoBar : Control
{
	[Export] public Label UnitNameLabel;
	[Export] public Label UnitHealthLabel;
	[Export] public HBoxContainer AbilityContainer;
	[Export] public PackedScene AbilityBtnScene;
	
	private Dictionary<StringName, AbilityBtn> _abilityButtonsMap = new();

	public override void _Ready()
	{
		EventBus.Instance.UnitSelected += CreateUnitBar;
		EventBus.Instance.UnitDeselected += (Unit _) => Visible = false;
	}
	
	public void CreateUnitBar(Unit unit)
	{
		Visible = true;
		_abilityButtonsMap.Clear();
		
		foreach (Node child in AbilityContainer.GetChildren()) child.QueueFree();
		
		UnitNameLabel.Text = unit.UnitName.ToString();
		UnitHealthLabel.Text = unit.UnitStats.Health.ToString();
		
		AbilityComponent abilityC = unit.AbilityC;
		abilityC.AbilityCast += OnAbiltyCast;
		
		foreach (AbilityData ability in abilityC.Abilities)
		{
			var newAbilityBtn = AbilityBtnScene.Instantiate<AbilityBtn>();
			newAbilityBtn.DisplayAbility(unit, ability);

			if (!abilityC.CanAfford(ability.Name))
			{
				GD.Print($"Ability {ability.Name} is not affordable.");
				newAbilityBtn.Disabled = true;
			}
			
			_abilityButtonsMap.Add(ability.Name, newAbilityBtn);
			AbilityContainer.AddChild(newAbilityBtn);
		}
	}

	private void OnAbiltyCast(Unit caster, AbilityData ability, EffectResult effectResult)
	{
		if (caster.AbilityC.CanAfford(ability.Name)) return;
		GD.Print($"Ability {ability.Name} is not affordable.");
		_abilityButtonsMap[ability.Name].Disabled = true;
	}
}
