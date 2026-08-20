#nullable enable
using System.Linq;
using AKidsDream.Abilities;
using AKidsDream.Commands;
using AKidsDream.GameBoard;
using AKidsDream.Managers.SaveSystems;
using AKidsDream.StateMachines;
using AKidsDream.Units.Resources;
using AKidsDream.Common.Logging;
using AKidsDream.Core.Teams;
using AKidsDream.Managers;
using AKidsDream.Core.Controllers;
using Godot;
using Serilog;
using TileData = AKidsDream.Managers.SaveSystem.Resources;

namespace AKidsDream.Controllers;

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
public readonly struct PlayerInteractionPayload(
    InputEvent inputEvent,
    TileData.TileData? tileAtMousePos,
    bool isLeftClickPressed,
    bool isRightClickPressed
)
{
    public readonly InputEvent InputEvent = inputEvent;
    public readonly TileData.TileData? TileAtMousePos = tileAtMousePos;
    public readonly bool IsLeftClickPressed = isLeftClickPressed;
    public readonly bool IsRightClickPressed = isRightClickPressed;

    public Unit? UnitAtMousePos => TileAtMousePos?.Unit;
    public Vector2I? TileLocationAtMousePos => TileAtMousePos?.TileLocation;
    public bool HasTile => TileAtMousePos is not null;
    public bool HasUnit => UnitAtMousePos is not null;
}

public partial class PlayerInteractionController : Node2D, IPlayerController
{
    [Export] public Board Board = null!;
    [Export] public AbilityVisualizer AbilityVisualizer = null!;
    [Export] public StateMachine StateMachine = null!;
    [Export] public CommandExecutor CommandExecutor = null!;

    public Unit? CurrentSelectedUnit;
    public AbilityData? CurrentSelectedAbility;
    public PlayerId PlayerId;

    private static readonly ILogger Log = GameLogger.For<PlayerInteractionController>();
    private bool _isMyTurn;

    public PlayerInteractionController()
    {
    }

    public PlayerInteractionController(PlayerControllerContext context, PlayerData playerData)
    {
        PlayerId = playerData.PlayerId;

        Board = context.Board;
        AbilityVisualizer = context.AbilityVisualizer;
        CommandExecutor = context.CommandExecutor;
    }

    public override void _Ready()
    {
        StateMachine = new StateMachine();
        AddChild(StateMachine);

        EventBus.Instance.AbilityBtnPressed += OnAbilityBtnPressed;
        EventBus.Instance.EndTurnButtonPressed += OnEndTurnButtonPressed;
        EventBus.Instance.UnitKilled += OnUnitKilled;

        StateMachine.AddState(new NoAbilitySelectedState(this));
        StateMachine.AddState(new OnAbilitySelectedState(this));
        StateMachine.AddState(new EnemyTurnObservationState(this));
        StateMachine.ChangeState(null, nameof(EnemyTurnObservationState), true);

        Log.Here().Info("PlayerController for {PlayerId} initialized", PlayerId);
    }

    public override void _ExitTree()
    {
        EventBus.Instance.AbilityBtnPressed -= OnAbilityBtnPressed;
        EventBus.Instance.EndTurnButtonPressed -= OnEndTurnButtonPressed;
        EventBus.Instance.UnitKilled -= OnUnitKilled;
    }

    public void StartTurn()
    {
        StateMachine.ChangeState(null, nameof(NoAbilitySelectedState), true);
        _isMyTurn = true;
    }

    public void EndTurn()
    {
        DeselectCurrentUnit();
        StateMachine.ChangeState(null, nameof(EnemyTurnObservationState), true);
        _isMyTurn = false;
    }

    // -- INPUT --

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_isMyTurn)
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
            Input.IsActionJustPressed(nameof(Global.InputMapActions.LeftClick)),
            Input.IsActionJustPressed(nameof(Global.InputMapActions.RightClick))
        );
    }

    // -- EVENT CALLBACKS --

    private void OnUnitKilled(Unit unit)
    {
        if (CurrentSelectedUnit == unit)
            DeselectCurrentUnit();
    }

    private void OnAbilityBtnPressed(Unit unit, AbilityData ability)
    {
        if (!_isMyTurn) return;
        CurrentSelectedAbility = ability;

        StateMachine.ChangeState(null, nameof(OnAbilitySelectedState), true);
    }

    private void OnEndTurnButtonPressed(int callerPlayerIdInt)
    {
        if (callerPlayerIdInt != PlayerId.Value) return;
        CommandExecutor.Execute(new EndTurnCommand(PlayerId));
    }

    // -- --

    public void SelectUnit(Unit unit)
    {
        if (CurrentSelectedUnit == unit)
            return;

        DeselectCurrentUnit();

        CurrentSelectedUnit = unit;
        CommandExecutor.Execute(new SelectUnitCommand(unit));
    }

    /// <summary>
    /// Deselects the current unit and clear the current ability
    /// </summary>
    public void DeselectCurrentUnit()
    {
        if (CurrentSelectedUnit is null)
            return;
        
        CommandExecutor.Execute(new DeselectUnitCommand(CurrentSelectedUnit));
        ClearCurrentAbility();
        CurrentSelectedUnit = null;
    }

    public void ClearCurrentAbility()
    {
        if (CurrentSelectedAbility is null)
            return;

        // Check if any unit is currently casting a blocking ability
        bool isAnyUnitCasting = Board.GetAllUnits().Any(unit => unit.AbilityC.IsCasting);

        if (!isAnyUnitCasting)
            AbilityVisualizer.ClearTilemaps();
        
        CurrentSelectedAbility = null;
        StateMachine.ChangeState(null, nameof(NoAbilitySelectedState), true);
    }
}