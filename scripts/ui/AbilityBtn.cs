using Godot;
using AKidsDream.Abilities;
using AKidsDream.Globals;
using AKidsDream.Units;

namespace AKidsDream.UI;

public partial class AbilityBtn : Control
{
	[Export] public Label AbilityName;
	[Export] public Button AbilityButton;
	public AbilityData Ability;
	public Unit Unit;

	public override void _Ready()
	{
		AbilityButton.Pressed += OnAbilityButtonPressed;
	}

	public void DisplayAbility(Unit unit, AbilityData ability)
	{
		Unit = unit;
		
		Ability = ability;
		AbilityName.Text = ability.Name;
		AbilityButton.Icon = ability.Icon;
		AbilityButton.TooltipText = ability.Description;
	}

	public void OnAbilityButtonPressed()
	{
		EventBus.Instance.EmitSignal(EventBus.SignalName.AbilityBtnPressed, Unit, Ability);
	}
}
