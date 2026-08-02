using AKidsDream.Abilities;
using Godot;
using AKidsDream.Managers.SaveSystems;
using AKidsDream.Units.Resources;

namespace AKidsDream.UnitInfoBar.UI;

public partial class AbilityButton : Control
{
	[Export] public Label AbilityName;
	[Export] public Button Button;
	public AbilityData Ability;
	public Unit Unit;

	private bool _disabled;
	public bool Disabled
	{
		get => _disabled;
		 set
		 {
			 _disabled = value;
			 Button.Disabled = value;
		 }
	}
	
	public override void _Ready()
	{
		Button.Pressed += OnAbilityButtonPressed;
	}

	public void DisplayAbility(Unit unit, AbilityData ability)
	{
		Unit = unit;
		
		Ability = ability;
		AbilityName.Text = ability.Name;
		Button.Icon = ability.Icon;
		Button.TooltipText = ability.Description;
	}
	
	private void OnAbilityButtonPressed()
	{
		EventBus.Instance.EmitSignal(EventBus.SignalName.AbilityBtnPressed, Unit, Ability);
	}
}
