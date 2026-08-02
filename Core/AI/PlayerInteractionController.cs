#nullable enable
using AKidsDream.Abilities;
using AKidsDream.Commands;
using AKidsDream.GameBoard;
using AKidsDream.Managers.SaveSystems;
using AKidsDream.StateMachines;
using AKidsDream.Units.Resources;
using AKidsDream.Common.Logging;
using Godot;
using Serilog;
using TileData = AKidsDream.Managers.SaveSystem.Resources;

namespace AKidsDream.Managers.AI;

/*
Handles Clicks based on Rules:

No Ability Selected:
1. Clicking the selected friendly Unit deselects it.
2. Clicking another friendly Unit selects that unit and deselects the previous one.
3. Clicking an enemy Unit shows stats.
4. Clicking an empty board does nothing.

Ability Selected:
1. Clicking outside the ability reach cancels the active ability.
   The click is consumed and does not select another unit on the same frame. (Config Option) // CONFIG:
2. Clicking inside the reach pattern targets that tile.
   Upon reaching Max Targets Selected, cast automatically. (Config Option) // CONFIG:
3. Hovering inside the reach pattern previews the effect.
4. After casting, the unit remains selected and ability state is cleared.
*/
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
    [Export] public Board Board = null!;
    [Export] public AbilityVisualizer AbilityVisualizer = null!;
    [Export] public StateMachine StateMachine = null!;
    [Export] public GameLoopManager GameLoopManager = null!;
    [Export] public CommandExecutor CommandExecutor = null!;

    private static readonly ILogger _log = GameLogger.For<PlayerInteractionController>(); 
    // CHECK:
    // When Implemented all logic
    // PlayerInteractionController has no logs, is that intended or should there be for something some logs? (or do the states already handle all logs when needed...

    public Unit? CurrentSelectedUnit;
    public AbilityData? CurrentSelectedAbility;

    public override void _Ready()
    {
        EventBus.Instance.AbilityBtnPressed += OnAbilityBtnPressed;
        EventBus.Instance.PlayerTurnEnded += () =>
        {
            ClearCurrentAbility();
            DeselectCurrentUnit();
        };

        StateMachine.AddState(new NoAbilitySelected(this));
        StateMachine.AddState(new OnAbilitySelected(this));
        StateMachine.ChangeState(null, nameof(NoAbilitySelected), true);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (GameLoopManager.PlayerPlayed)
            return;
        // User still may want information on the Enemy/its own units, how to handle that (more states :///)
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
        if (GameLoopManager.PlayerPlayed) return;
        CurrentSelectedAbility = ability;

        CommandExecutor.Execute(new SelectAbilityCommand(
            unit,
            ability.Name
        ));

        StateMachine.ChangeState(null, nameof(OnAbilitySelected), true);
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
        StateMachine.ChangeState(null, nameof(NoAbilitySelected), true);
    }
}