#nullable enable
using System;
using System.Linq;
using AKidsDream.Abilities;
using AKidsDream.Commands;
using AKidsDream.Common.Logging;
using AKidsDream.StateMachines;
using AKidsDream.Units.Resources;
using AKidsDream.Units.Resources.Components;
using Godot;
using Serilog;

namespace AKidsDream.Controllers;

/*
Ability Selected:
1. Clicking outside the ability reach cancels the active ability.
The click is consumed and does not select another unit on the same frame. (Config Option) // CONFIG:
2. Clicking inside the reach pattern targets that tile.
Upon reaching Max Targets Selected, cast automatically. (Config Option) // CONFIG:
3. Hovering inside the reach pattern previews the effect.
4. After casting, the unit remains selected and ability state is cleared.
*/
public class OnAbilitySelectedState(PlayerInteractionController pic) : IState
{
	
	
	public Action<IState, string, bool> ChangeState { get; set; } = null!;

	private AbilityData _ability = null!;
	private Unit _caster = null!;
	private AbilityContext _abilityContext = null!;
	private AbilityPayload _abilityPayload = null!;
	private static readonly ILogger Log = GameLogger.For<OnAbilitySelectedState>();
	private Vector2I? _lastHoveredTile = null;

	public void Enter()
	{
		_ability = pic.CurrentSelectedAbility!;
		_caster = pic.CurrentSelectedUnit!;

		_abilityPayload = _caster.AbilityC.CreatePayload(
			_ability.Name,
			[],
			pic.Board
		);
		_abilityContext = new AbilityContext
		{
			Source = _caster,
			Ability = _ability,
			Board = pic.Board
		};

		pic.CommandExecutor.Execute(new SelectAbilityBaseCommand(
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
		
		if (interaction is { IsLeftClickPressed: true, HasTile: true })
		{
			HandleLeftClick(interaction);
			return;
		}

		HandleHover(interaction);
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

	private void HandleHover(PlayerInteractionPayload interaction)
	{
		if (_lastHoveredTile == interaction.TileLocationAtMousePos)
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
		var result = pic.CommandExecutor.Execute(new AddAbilityTargetBaseCommand(
			_caster,
			_ability.Name,
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
		await pic.CommandExecutor.ExecuteAsync(new CastAbilityBaseCommand(
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
