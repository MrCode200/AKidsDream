#nullable enable
using Godot;
using AKidsDream.Abilities;
using AKidsDream.Common.Logging;
using AKidsDream.Core.Managers;
using AKidsDream.Managers.SaveSystems;
using AKidsDream.Units.Resources;
using Godot.Collections;
using Serilog;

namespace AKidsDream.UnitInfoBar.UI;

public partial class UnitInfoBar : Control, IBlockable
{
    [Export]
    public required Array<BlockingStrategy> Strategies { get; set; } =
    [
        BlockingStrategy.BlockOnBlockingTrigger,
        BlockingStrategy.BlockOnEffectApply
    ];

    [Export] public Label UnitNameLabel = null!;
    [Export] public Label UnitHealthLabel = null!;
    [Export] public HBoxContainer AbilityContainer = null!;
    [Export] public PackedScene AbilityBtnScene = null!;
    [Export] public PoolBar PoolBar = null!;

    private readonly ILogger _log = GameLogger.For<UnitInfoBar>();

    private Dictionary<StringName, AbilityButton> _abilityButtonsMap = new();
    private Unit? _selectedUnit;
    public bool IsBlocked { get; set; }


    private void OnUnitDeselected(Unit _) => Visible = false;

    public override void _Ready()
    {
        BlockingManager.Instance.Register(this);

        EventBus.Instance.UnitSelected += CreateUnitBar;
        EventBus.Instance.UnitDeselected += OnUnitDeselected;
    }

    public override void _ExitTree()
    {
        EventBus.Instance.UnitSelected -= CreateUnitBar;
        EventBus.Instance.UnitDeselected -= OnUnitDeselected;
    }
    
    // -- SIGNAL HANDLING --

    private void CreateUnitBar(Unit unit)
    {
        Visible = true;
        // Move if needed to Visible get set method 
        foreach (var btn in _abilityButtonsMap.Values)
        {
            btn.SpawnAnimation();
        }

        
        PoolBar.SetPool(unit);
        if (_selectedUnit?.UnitId == unit.UnitId)
        {
            return;
        }

        _selectedUnit = unit;

        _abilityButtonsMap.Clear();

        foreach (Node child in AbilityContainer.GetChildren()) child.QueueFree();

        UnitNameLabel.Text = _selectedUnit.UnitName.ToString();
        UnitHealthLabel.Text = _selectedUnit.UnitStats.Health.ToString();

        var abilityC = _selectedUnit.AbilityC;

        foreach (AbilityData ability in abilityC.Abilities.Values)
        {
            var newAbilityBtn = AbilityBtnScene.Instantiate<AbilityButton>();
            newAbilityBtn.DisplayAbility(_selectedUnit, ability);
            // if false, button when running spawn animation will show its enabled version, if should be disabled
            newAbilityBtn.Disabled = true; 
            
            _abilityButtonsMap.Add(ability.Name, newAbilityBtn);
            AbilityContainer.AddChild(newAbilityBtn);
        }

        _UpdateButtonStates();
    }
    
    // -- SIGNAL HANDLING --

    private void _UpdateButtonStates(bool enableBtns = true)
    {
        foreach (var btn in _abilityButtonsMap.Values)
        {
            if (enableBtns && !IsBlocked && _selectedUnit != null)
            {
                btn.Disabled = false;
            }
            else
            {
                btn.Disabled = true;
            }
        }
    }

    public void SetBlocked(bool block)
    {
        if (block == IsBlocked) return;
        IsBlocked = block;

        _UpdateButtonStates(!block);
    }
    
}