#nullable enable
using System.Linq;
using Godot;
using AKidsDream.Abilities;
using AKidsDream.Common.Logging;
using AKidsDream.Core.Managers;
using AKidsDream.Managers.SaveSystems;
using AKidsDream.Common;
using AKidsDream.Common.Components.TweenComponent.Resources;
using AKidsDream.Res.Common.Components.TweenComponent.Resources;
using AKidsDream.Utilities;
using Godot.Collections;
using Serilog;

namespace AKidsDream.UnitInfoBar.UI;

// TODO:
// remake how UnitSeleciton is handled, add UpdateUnitInfoBar Method
// If new unit is selected deselect old unit silently?/new signal for unit changed
// When updated play animation only if deselected with no other unit selected,
public partial class UnitInfoBar : Control, IBlockable
{
	[Export]
	public required Array<BlockingStrategy> BlockingStrategies { get; set; } =
	[
		BlockingStrategy.BlockOnBlockingTrigger,
		BlockingStrategy.BlockOnEffectApply
	];

	[Export] public Label UnitNameLabel = null!;
	[Export] public Label UnitHealthLabel = null!;
	[Export] public HBoxContainer AbilityContainer = null!;
	[Export] public PackedScene AbilityBtnScene = null!;
	[Export] public PoolBar PoolBar = null!;
	[Export] public TweenComponent SpawnTweenComponent = null!;

	private readonly ILogger _log = GameLogger.For<UnitInfoBar>();
	
	private Dictionary<StringName, AbilityButton> _abilityButtonsMap = new();
	private Unit? _selectedUnit;
	public bool IsBlocked { get; set; }


	private void OnUnitDeselected(Unit _)
	{
		_selectedUnit = null;
		SpawnTweenComponent.PlayTween(TweenAnimationIdentifiers.UIBOnHide);
	}

	public override void _Ready()
	{
		foreach (Node child in AbilityContainer.GetChildren()) child.QueueFree();
		BlockingManager.Instance.Register(this);

		EventBus.Instance.UnitSelected += CreateUnitBar;
		EventBus.Instance.UnitDeselected += OnUnitDeselected;
		EventBus.Instance.UnitChanged += UpdateUnitBar;
	}

	public override void _ExitTree()
	{
		BlockingManager.Instance.Unregister(this);
		EventBus.Instance.UnitSelected -= CreateUnitBar;
		EventBus.Instance.UnitDeselected -= OnUnitDeselected;
		EventBus.Instance.UnitChanged -= UpdateUnitBar;
	}
	
	// -- SIGNAL HANDLING --
	private void CreateUnitBar(Unit unit)
	{
		Visible = true;
		SpawnTweenComponent.PlayTween(TweenAnimationIdentifiers.UIBOnShow);
		// _abilityButtonsMap.Clear();

		// foreach (Node child in AbilityContainer.GetChildren()) child.QueueFree();
		
		UpdateUnitBar(_selectedUnit, unit);
	}
	
	private void UpdateUnitBar(Unit? _, Unit newUnit)
	{
		_selectedUnit = newUnit;
		PoolBar.SetPool(newUnit);

		UnitNameLabel.Text = newUnit.UnitName.ToString();
		UnitHealthLabel.Text = newUnit.UnitStats.Health.ToString();

		// Update ability buttons using ZipLongest to handle additions/removals/updates
		var newAbilities = newUnit.AbilityC.Abilities.Values.ToList();
		
		foreach (var (abilityButton, newAbility) in Utils.ZipLongest(_abilityButtonsMap.Values, newAbilities))
		{
			if (abilityButton is null && newAbility is not null)
			{
				// Create new ability button
				var newAbilityBtn = AbilityBtnScene.Instantiate<AbilityButton>();
				newAbilityBtn.DisplayAbility(newUnit, newAbility);
				newAbilityBtn.Disabled = true;
				_abilityButtonsMap.Add(newAbility.Name, newAbilityBtn);
				AbilityContainer.AddChild(newAbilityBtn);
			}
			else if (abilityButton is not null && newAbility is null)
			{
				// Remove button
				_abilityButtonsMap.Remove(abilityButton.Ability.Name);
				abilityButton.QueueFree();
			}
			else if (abilityButton is not null && newAbility is not null)
			{
				// Update existing button and remap if name changed
				if (abilityButton.Ability.Name != newAbility.Name)
				{
					_abilityButtonsMap.Remove(abilityButton.Ability.Name);
					_abilityButtonsMap.Add(newAbility.Name, abilityButton);
				}
				abilityButton.DisplayAbility(newUnit, newAbility);
			}
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
