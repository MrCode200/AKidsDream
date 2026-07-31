using AKidsDream.Abilities;
using Godot;
using AKidsDream.Managers.SaveSystems;
using AKidsDream.Units.Resources;

namespace AKidsDream.UnitInfoBar.UI;

public partial class AbilityBtn : Control
{
	[Export] public Label AbilityName;
	[Export] public Button AbilityButton;
	public AbilityData Ability;
	public Unit Unit;

	private bool _disabled;
	public bool Disabled
	{
		get => _disabled;
		 set
		 {
			 _disabled = value;
			 AbilityButton.Disabled = value;
		 }
	}
	
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
	
	private void OnAbilityButtonPressed()
	{
		EventBus.Instance.EmitSignal(EventBus.SignalName.AbilityBtnPressed, Unit, Ability);
	}
}
