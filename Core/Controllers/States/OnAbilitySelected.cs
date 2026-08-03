#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using AKidsDream.Abilities;
using AKidsDream.Commands;
using AKidsDream.Common.Logging;
using AKidsDream.StateMachines;
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
public class OnAbilitySelected(PlayerInteractionController pic) : IState
{
    public Action<IState, string, bool> ChangeState { get; set; } = null!;

    private AbilityData? _ability;
    private Vector2I[] _reachTiles = [];
    private readonly List<Vector2I> _targetedTiles = [];
    private static readonly ILogger Log = GameLogger.For<OnAbilitySelected>();

    public void Enter()
    {
        _ability = pic.CurrentSelectedAbility;
        _reachTiles = [];
        _targetedTiles.Clear();
    }

    public void Update(object? payload)
    {
        if (payload is not PlayerInteractionPayload interaction)
            return;

        if (!HasValidAbilityContext())
        {
            CancelAbilityFromClick();
            return;
        }

        EnsureReachTilesLoaded();
        if (!interaction.HasTile) return;

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
            Log.Here().Debug(
                "Tile {TileLocation} already targeted for ability '{AbilityName}'",
                tileLocation,
                _ability!.Name);
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
        {
            Log.ForContext("UnitName", pic.CurrentSelectedUnit?.UnitName)
                .ForContext("UnitId", pic.CurrentSelectedUnit?.UnitId)
                .Here()
                .Debug("Max targets reached for ability '{AbilityName}', Auto-Casting...", _ability.Name);
            CastAbility();
        }
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

        pic.ClearCurrentAbility(); // StateChange is called in that function
    }

    private void CancelAbilityFromClick()
    {
        Log.Here().Debug(
            "Ability '{AbilityName}' cancelled due to click outside reach for unit '{UnitName}' (id: {UnitId})",
            _ability?.Name,
            pic.CurrentSelectedUnit?.UnitName,
            pic.CurrentSelectedUnit?.UnitId);
        pic.ClearCurrentAbility(); // StateChange is called in that function
        pic.GetViewport().SetInputAsHandled(); // So that on click doesn't select Unit again when canceled.
    }
}