using AKidsDream.Abilities;
using Godot;
using AKidsDream.Managers.SaveSystems;
using AKidsDream.Units.Resources;

namespace AKidsDream.UnitInfoBar.UI;

public partial class AbilityButton : Control
{
	[Export] public Control ContentContainer;
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
		
		ContentContainer.Scale = Vector2.Zero;
		ContentContainer.Rotation = Mathf.Pi * 1.5f;
		Tween tween = CreateTween();
		tween.SetParallel();
		
		tween.TweenProperty(ContentContainer, "scale", new Vector2(1f, 1f), 0.5f)
			.SetEase(Tween.EaseType.Out)
			.SetTrans(Tween.TransitionType.Back);
		tween.TweenProperty(ContentContainer, "rotation", Mathf.Tau, 0.4f)
			.SetEase(Tween.EaseType.Out)
			.SetTrans(Tween.TransitionType.Cubic);
	}

	public override void _ExitTree()
	{
		Button.Pressed -= OnAbilityButtonPressed;
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
