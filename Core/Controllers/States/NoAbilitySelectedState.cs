#nullable enable
using System;
using AKidsDream.Core.Managers;
using AKidsDream.Managers;
using AKidsDream.Managers.SaveSystems;
using AKidsDream.StateMachines;
using AKidsDream.Units.Resources;
using Serilog;

namespace AKidsDream.Controllers;

/*
Handles Clicks based on Rules:

No Ability Selected:
1. Clicking the selected friendly Unit deselects it.
2. Clicking another friendly Unit selects that unit and deselects the previous one.
3. Clicking an enemy Unit shows stats.
4. Clicking an empty board does nothing.
*/
public class NoAbilitySelectedState(PlayerInteractionController pic) : IState
{
    public Action<IState, string, bool>? ChangeState { get; set; } = null!;

    //Handles Clicks based on Rules:
    public void Update(object? payload)
    {
        if (payload is not PlayerInteractionPayload interaction)
            return;

        if (!interaction.IsLeftClickPressed)
            return;

        HandleLeftClick(interaction);
    }

    private void HandleLeftClick(PlayerInteractionPayload interaction)
    {
        if (!interaction.HasUnit)
            return;

        Unit clickedUnit = interaction.UnitAtMousePos!;

        // 1. Clicking the selected friendly Unit deselects it.
        if (clickedUnit == pic.CurrentSelectedUnit)
        {
            pic.DeselectCurrentUnit();
            return;
        }

        // 2. Clicking another friendly Unit selects that unit and deselects the previous one.
        if (!GameManager.Instance.PlayerTeamRegistry.IsHostileToPlayer(pic.PlayerId, clickedUnit.OwnerId))
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