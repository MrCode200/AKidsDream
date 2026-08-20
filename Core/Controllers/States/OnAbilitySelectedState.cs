#nullable enable
using System;
using System.Linq;
using AKidsDream.Abilities;
using AKidsDream.Commands;
using AKidsDream.Common.Logging;
using AKidsDream.Managers;
using AKidsDream.StateMachines;
using AKidsDream.Units.Resources;
using AKidsDream.Units.Resources.Components;
using Godot;
using Serilog;

namespace AKidsDream.Controllers;

/*
Ability Selected:
1. Left-Clicking outside the ability reach cancels the active ability.
The click is consumed and does not select another unit on the same frame. (Config Option) // CONFIG:
2. Left-Clicking inside the reach pattern targets that tile.
Upon reaching Max Targets Selected, cast automatically. (Config Option) // CONFIG:
3. Right-Clicking a selected tile, deselects that tile. If only one tile is selected, the ability is canceled.
4. Hovering inside the reach pattern previews the effect. (removes tile, order remains same)
5. After casting, the unit remains selected and ability state is cleared.
*/
public class OnAbilitySelectedState(PlayerInteractionController pic) : IState
{
	public Action<IState, string, bool> ChangeState { get; set; } = null!;

	private AbilityData _ability = null!;
	private Unit _caster = null!;
	private AbilityContext _abilityContext = null!;
	private AbilityPayload _abilityPayload = null!;
	private static readonly ILogger Log = GameLogger.For<OnAbilitySelectedState>();
	private Vector2I? _lastHoveredTile;

	public void Enter()
	{
		pic.AbilityVisualizer.ClearTilemaps();
		
		_ability = pic.CurrentSelectedAbility!;
		_caster = pic.CurrentSelectedUnit!;

		_abilityPayload = _caster.AbilityC.CreatePayload(
			_ability.Name,
			[],
			pic.Board
		);
		_abilityContext = new AbilityContext
		{
			Caster = _caster,
			Ability = _ability,
			Board = pic.Board
		};

		pic.CommandExecutor.Execute(new SelectAbilityCommand(
			_caster,
			_ability.Name,
			_abilityContext,
			_abilityPayload
		));
	}

	public void Update(object? payload)
	{
		if (payload is not PlayerInteractionPayload interaction)
			return;

		switch (interaction)
		{
			case { IsLeftClickPressed: true, HasTile: true }:
				HandleLeftClick(interaction);
				break;
			case { IsRightClickPressed: true, HasTile: true }:
				HandleRightClick(interaction);
				break;
			default:
				HandleHover(interaction);
				break;
		}

		if (_caster.AbilityC.IsCasting)
		{
			// REMOVE HIGHLIGHTS OF ALREADY PROCESSED TILES
		}
	}

	public void Exit()
	{
		_abilityContext = null!;
		_abilityPayload = null!;
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

	private void HandleRightClick(PlayerInteractionPayload interaction)
	{
		// Prevent tile removal while ability is being cast
		if (_caster.AbilityC.IsCasting)
		{
			Log.Here().Debug(
				"Cannot remove target - ability '{AbilityName}' is currently being cast for unit '{UnitName}' (id: {UnitId})",
				_ability.Name,
				_caster.UnitName,
				_caster.UnitId);
			return;
		}

		// 3. Right-clicking a selected tile deselects that tile.
		if (_abilityPayload.AccumulatedTargets.Count == 1)
		{
			CancelAbilityFromClick();
			return;
		}
		
		pic.CommandExecutor.Execute(new RmvAbilityTargetCommand(
			interaction.TileAtMousePos!.TileLocation,
			_abilityContext,
			_abilityPayload
		));
	}

	private void HandleHover(PlayerInteractionPayload interaction)
	{
		if (_caster.AbilityC.IsCasting || _lastHoveredTile == interaction.TileLocationAtMousePos)
			return;

		_lastHoveredTile = interaction.TileLocationAtMousePos;
		if (!IsTileInsideReach(interaction.TileLocationAtMousePos))
		{
			// Show effect visualization for all effects
			pic.AbilityVisualizer.ShowEffectVisualization(
				_abilityContext,
				_abilityPayload,
				_ability.Effects
			);
			return;
		}

		//Show Targets with Hover - create temporary payload for preview
		var previewPayload = _abilityPayload.Copy();
		previewPayload.ProcessingTiles.Add(interaction.TileLocationAtMousePos!.Value);
		previewPayload.AccumulatedTargets.Add(interaction.TileLocationAtMousePos!.Value);

		// Show effect visualization for all effects
		pic.AbilityVisualizer.ShowEffectVisualization(
			_abilityContext,
			previewPayload,
			_ability.Effects
		);
	}

	private void HandleReachTileClick(Vector2I targetedTile)
	{
		// Visualizes the Effect tiles
		var result = pic.CommandExecutor.Execute(new AddAbilityTargetCommand(
			targetedTile,
			_abilityContext,
			_abilityPayload
		));

		if (result.FailureType is not CommandFailureType.None)
			return; // logs in command executor

		// Recalculate and update reach visualization after target addition
		pic.AbilityVisualizer.ShowReachVisualization(_abilityContext, _abilityPayload, _ability);

		// If reached Max Targets, cast the ability.
		if (_abilityPayload.AccumulatedTargets.Count >= _ability.MaxTargets)
		{
			Log.ForContext("UnitName", _caster.UnitName)
				.ForContext("UnitId", _caster.UnitId)
				.Here()
				.Debug("Max targets reached for ability '{AbilityName}', Auto-Casting...", _ability.Name);
			CastAbilityAsync();
		}
	}

	// -- HELPERS --
	private bool IsTileInsideReach(Vector2I? tileLocation)
	{
		if (tileLocation is null) return false;
		var reachData = _ability.GetReachVisualizationData(_abilityContext, _abilityPayload);
		return reachData.tiles.Contains(tileLocation.Value);
	}

	private async void CastAbilityAsync()
	{
		pic.AbilityVisualizer.ClearReachTilemap();
		
		_ = pic.CommandExecutor.ExecuteAsync(new CastAbilityBaseCommand(
			_caster,
			_ability.Name,
			_abilityContext,
			_abilityPayload
		));

		pic.ClearCurrentAbility(); // StateChange is called in that function
	}

	private void CancelAbilityFromClick()
	{
		Log.Here().Debug(
			"Ability '{AbilityName}' cancelled due to click outside reach for unit '{UnitName}' (id: {UnitId})",
			_ability.Name,
			_caster.UnitName,
			_caster.UnitId);
		pic.ClearCurrentAbility(); // StateChange is called in that function
		pic.GetViewport().SetInputAsHandled(); // So that on click doesn't select Unit again when canceled.
	}
}
