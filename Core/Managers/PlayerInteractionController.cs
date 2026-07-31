#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using AKidsDream.Abilities;
using AKidsDream.Commands;
using AKidsDream.GameBoard;
using AKidsDream.Managers.SaveSystems;
using AKidsDream.StateMachines;
using AKidsDream.Units.Resources;
using Godot;
using TileData = AKidsDream.Managers.SaveSystem.Resources;

namespace AKidsDream.Managers;

/*
Handles Clicks based on Rules:

No Ability Selected:
1. Clicking the selected friendly Unit deselects it.
2. Clicking another friendly Unit selects that unit and deselects the previous one.
3. Clicking an enemy Unit shows stats.
4. Clicking an empty board does nothing.

Ability Selected:
1. Clicking outside the ability reach cancels the active ability.
   The click is consumed and does not select another unit on the same frame. (Config Option)
2. Clicking inside the reach pattern targets that tile.
   Upon reaching Max Targets Selected, cast automatically. (Config Option)
3. Hovering inside the reach pattern previews the effect.
4. After casting, the unit remains selected and ability state is cleared.
*/
// TODO:
// Either disable button, or put incode check if AbilityC.CanAfford(ability) -> bool
// So it doesn't show the ability and thus Hover, if it the player can't afford it anyways
public sealed class PlayerInteractionPayload(
	InputEvent inputEvent,
	TileData.TileData? tileAtMousePos,
	bool isLeftClickPressed
)
{
	public readonly InputEvent InputEvent = inputEvent;
	public readonly TileData.TileData? TileAtMousePos = tileAtMousePos;
	public readonly bool IsLeftClickPressed = isLeftClickPressed;

	public Unit? UnitAtMousePos => TileAtMousePos?.Unit;
	public Vector2I? TileLocationAtMousePos => TileAtMousePos?.TileLocation;
	public bool HasTile => TileAtMousePos is not null;
	public bool HasUnit => UnitAtMousePos is not null;
}

public partial class PlayerInteractionController : Node2D
{
	[Export] public required Board Board;
	[Export] public required AbilityVisualizer AbilityVisualizer;
	[Export] public required StateMachine StateMachine;
	[Export] public CommandExecutor CommandExecutor;

	public Unit? CurrentSelectedUnit;
	public AbilityData? CurrentSelectedAbility;

	public override void _Ready()
	{
		EventBus.Instance.AbilityBtnPressed += OnAbilityBtnPressed;

		StateMachine.AddState(new NoAbilitySelected(this));
		StateMachine.AddState(new OnAbilitySelected(this));
		StateMachine.ChangeState(null, "NoAbilitySelected", true);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		StateMachine.Update(CreatePayload(@event));
	}

	private PlayerInteractionPayload CreatePayload(InputEvent @event)
	{
		var tile = Board.WorldPositionToTile(GetGlobalMousePosition());

		return new PlayerInteractionPayload(
			@event,
			tile,
			Input.IsActionJustPressed(nameof(Global.InputMapActions.LeftClick))
		);
	}

	private void OnAbilityBtnPressed(Unit unit, AbilityData ability)
	{
		CurrentSelectedAbility = ability;

		CommandExecutor.Execute(new SelectAbilityCommand(
			unit,
			ability.Name
		));

		StateMachine.ChangeState(null, "OnAbilitySelected", true);
	}

	public void SelectUnit(Unit unit)
	{
		if (CurrentSelectedUnit == unit)
			return;


		DeselectCurrentUnit();

		CurrentSelectedUnit = unit;
		CommandExecutor.Execute(new SelectUnitCommand(unit));
	}

	public void DeselectCurrentUnit()
	{
		if (CurrentSelectedUnit is not null)
			CommandExecutor.Execute(new DeselectUnitCommand(CurrentSelectedUnit));

		CurrentSelectedUnit = null;
	}

	public void ClearCurrentAbility()
	{
		CurrentSelectedAbility = null;
		CommandExecutor.Execute(new DeselectAbilityCommand(CurrentSelectedUnit));
	}
}

public class NoAbilitySelected(PlayerInteractionController pic) : IState
{
	public Action<IState, string, bool>? ChangeState { get; set; }

	//Handles Clicks based on Rules:
	public void Update(Object payload)
	{
		if (payload is not PlayerInteractionPayload interaction)
			return;

		if (!interaction.IsLeftClickPressed)
			return;

		HandleLeftClick(interaction);
	}

	private void HandleLeftClick(PlayerInteractionPayload interaction)
	{
		Unit? clickedUnit = interaction.UnitAtMousePos;

		if (clickedUnit is null)
			return;

		// 1. Clicking the selected friendly Unit deselects it.
		if (clickedUnit == pic.CurrentSelectedUnit)
		{
			pic.DeselectCurrentUnit();
			return;
		}

		// 2. Clicking another friendly Unit selects that unit and deselects the previous one.
		if (clickedUnit.Team == Global.UnitTeam.Player)
		{
			pic.SelectUnit(clickedUnit);
			return;
		}

		// 3. Clicking an enemy Unit shows stats.
		ShowEnemyStats(clickedUnit);
	}

	private void ShowEnemyStats(Unit enemy)
	{
		// TODO: Show enemy stats.
	}
}

public class OnAbilitySelected(PlayerInteractionController pic) : IState
{
	public Action<IState, string, bool> ChangeState { get; set; }

	private AbilityData? _ability;
	private Vector2I[] _reachTiles = [];
	private readonly List<Vector2I> _targetedTiles = [];


	public void Enter()
	{
		_ability = pic.CurrentSelectedAbility;
		_reachTiles = [];
		_targetedTiles.Clear();
	}

	public void Update(object payload)
	{
		if (payload is not PlayerInteractionPayload interaction)
			return;

		if (!HasValidAbilityContext())
		{
			CancelAbilityFromClick();
			return;
		}

		EnsureReachTilesLoaded();

		if (interaction.IsLeftClickPressed)
		{
			HandleLeftClick(interaction);
			return;
		}

		HandleHover(interaction);
	}

	public void Exit()
	{
		_reachTiles = [];
		_targetedTiles.Clear();
	}

	// -- HANDLERS --
	private void HandleLeftClick(PlayerInteractionPayload interaction)
	{
		// 1. Clicking outside the ability reach cancels the active ability.
		// The click is consumed and does not select another unit on the same frame.
		if (!IsTileInsideReach(interaction.TileLocationAtMousePos))
		{
			CancelAbilityFromClick();
			return;
		}

		// 2. Clicking inside the reach pattern targets that tile.
		HandleReachTileClick(interaction.TileLocationAtMousePos!.Value);
	}

	private void HandleHover(PlayerInteractionPayload interaction)
	{
		if (!IsTileInsideReach(interaction.TileLocationAtMousePos))
		{
			return;
		}

		//Show Targets with Hover
		pic.AbilityVisualizer.ShowEffectVisualization(
			pic.CurrentSelectedUnit!,
			[
				.. _targetedTiles,
				interaction.TileLocationAtMousePos!.Value
			],
			_ability!.Effect
		);
	}

	private void HandleReachTileClick(Vector2I tileLocation)
	{
		if (ViolatesDuplicateTargetRule(tileLocation))
		{
			GD.PrintErr("Tile already targeted");
			return;
		}

		// Adds if !ViolatesDuplicateTargetRule target to _targetedTiles list
		// Visualizes the Effect tiles
		pic.CommandExecutor.Execute(new AddAbilityTargetCommand(
			pic.CurrentSelectedUnit,
			_ability!.Name,
			tileLocation,
			_targetedTiles
		));

		// If reached Max Targets, cast the ability.
		if (_targetedTiles.Count >= _ability!.Effect.MaxTargets)
			CastAbility();
	}

	// -- HELPERS --

	private bool HasValidAbilityContext()
	{
		return _ability is not null && pic.CurrentSelectedUnit is not null;
	}

	private void EnsureReachTilesLoaded()
	{
		if (_reachTiles.Length != 0) return;

		_reachTiles = _ability!.GetReachVisualizationData(
			pic.CurrentSelectedUnit!,
			pic.Board,
			pic.CurrentSelectedUnit!.TileLocation
		).tiles;
	}

	private bool IsTileInsideReach(Vector2I? tileLocation)
	{
		return tileLocation is not null && _reachTiles.Contains(tileLocation.Value);
	}

	private bool ViolatesDuplicateTargetRule(Vector2I tileLocation)
	{
		return !_ability!.Effect.AllowDuplicateTiles &&
			   _targetedTiles.Contains(tileLocation);
	}

	private void CastAbility()
	{
		pic.CommandExecutor.Execute(new CastAbilityCommand(
			pic.CurrentSelectedUnit!,
			_ability!.Name,
			[.. _targetedTiles]
		));

		pic.ClearCurrentAbility();
		ChangeState(this, "NoAbilitySelected", false);
	}

	private void CancelAbilityFromClick()
	{
		pic.ClearCurrentAbility();
		pic.GetViewport().SetInputAsHandled(); // So that on click doesn't select Unit again, when cancelled.
		ChangeState(this, "NoAbilitySelected", false);
	}
}
