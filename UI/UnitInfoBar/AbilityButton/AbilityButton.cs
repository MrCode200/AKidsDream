using AKidsDream.Abilities;
using AKidsDream.Abilities.Effects;
using Godot;
using AKidsDream.Managers.SaveSystems;
using AKidsDream.Common;

namespace AKidsDream.UnitInfoBar.UI;

public partial class AbilityButton : Control
{
    [Export] public Control ContentContainer;
    [Export] public Label AbilityName;
    [Export] public Button Button;
    public AbilityData Ability;
    public Unit Unit;

    private bool _cannotAfford;
    private bool _disabled;

    public bool Disabled
    {
        get => _disabled;
        set
        {
            _disabled = _cannotAfford ? true : value;
            GD.Print("Disabled: " + _disabled);
            GD.Print("Cannot afford: " + _cannotAfford);
            Button.Disabled = _disabled;
        }
    }

    public override void _Ready()
    {
        Button.Pressed += OnAbilityButtonPressed;
    }

    public override void _ExitTree()
    {
        Button.Pressed -= OnAbilityButtonPressed;
        if (Ability != null)
            Ability.AbilityCast -= CheckCanAffordCast;
    }

    public void DisplayAbility(Unit unit, AbilityData ability)
    {
        // TODO: Display-switching tween animation
        Unit = unit;

        if (Ability != null)
            Ability.AbilityCast -= CheckCanAffordCast;

        Ability = ability;
        AbilityName.Text = ability.Name;
        Button.Icon = ability.Icon;
        Button.TooltipText = ability.Description;

        Ability.AbilityCast += CheckCanAffordCast;

        CheckCanAffordCast(ability);
    }

    private void OnAbilityButtonPressed()
    {
        EventBus.Instance.EmitSignal(EventBus.SignalName.AbilityBtnPressed, Unit, Ability);
    }

    private void CheckCanAffordCast(AbilityData ability, EffectResult _ = null)
    {
        if (!ability.CanReplenishPool() &&
            Unit.AbilityC.TryCanAffordBaseCost(ability.Name, out var canAfford) &&
            !canAfford)
        {
            _cannotAfford = true;
            Disabled = true;
        }
        else if (_cannotAfford)
            _cannotAfford = false;
    }
}