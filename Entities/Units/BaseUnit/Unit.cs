using AKidsDream.Common.Logging;
using AKidsDream.GameBoard;
using AKidsDream.Managers.SaveSystems;
using AKidsDream.Units.Resources.Components;
using AKidsDream.Utilities;
using Godot;
using Serilog;

namespace AKidsDream.Units.Resources;

/// <summary>
/// Base class for all unit types in the game.
/// Handles initialization & movement validation.
/// </summary>
[GlobalClass]
[Tool]
public partial class Unit : CharacterBody2D
{
    // -- PROPERTIES --
    public int UnitId { get; private set; }

    [Export] public Global.UnitName UnitName;
    [Export] public Global.UnitTeam Team;

    /// <summary>
    /// The current tile location of the unit.
    /// </summary>
    [Export] public Vector2I TileLocation;

    [Export] public UnitStatsData UnitStats;

    [Export] public StringName OnMoveCallEventBus;

    [Signal]
    public delegate void UnitMovedEventHandler(Unit unit, Vector2I from, Vector2I to);

    public HealthComponent HealthC { get; private set; }
    public SelectableComponent SelectableC { get; private set; }
    public DeathComponent DeathC { get; private set; }
    public AbilityComponent AbilityC { get; private set; }

    private ILogger _log = GameLogger.For<Unit>();

    public Unit()
    {
    }

    public void Init(
        Global.UnitName unitName,
        Global.UnitTeam team,
        Vector2I tileLocation,
        UnitStatsData unitStats,
        int unitId = 0
    )
    {
        var externalIdPassed = false;
        
        if (unitId == 0)
            UnitId = Utils.GetNextId();
        else
        {
            externalIdPassed = true;
            UnitId = unitId;
        }

        UnitName = unitName;
        Team = team;
        TileLocation = tileLocation;
        if (unitStats is not null) UnitStats = unitStats;

        // Set static context for all future log calls
        _log = _log.ForContext("UnitId", UnitId)
            .ForContext("UnitName", UnitName)
            .ForContext("Team", Team);
        
        if (externalIdPassed)
            _log.ForContext("TileLocation", TileLocation)
                .Here()
                .Warn(
                    "Unit Initialized With External Id at {TileLocation} with ID: {UnitId}",
                    TileLocation);
    }

    // -- LOGIC --

    public override void _Ready()
    {
        Position = Board.TileToWorldPosition(TileLocation);
        _setEnemyLogicAndAppearance();

        if (Engine.IsEditorHint()) return;

        _injectReferenceAndAssignComponents();

        AddToGroup(nameof(Global.Groups.Units));
        AddToGroup((Team == Global.UnitTeam.Enemy)
            ? nameof(Global.Groups.EnemyUnits)
            : nameof(Global.Groups.PlayerUnits)
        );
        EventBus.Instance.EmitSignal(EventBus.SignalName.UnitCreated, this);
        // _log.ForContext("TileLocation", TileLocation) // Too verbose, instead, log when creating/spawning Unit(s)...
        //    .Here()
        //    .Info("UnitReady at {TileLocation}", TileLocation);
    }

    private void _injectReferenceAndAssignComponents()
    {
        DeathC = GetNode<DeathComponent>("DeathComponent");
        SelectableC = GetNode<SelectableComponent>("SelectableComponent");
        AbilityC = GetNode<AbilityComponent>("AbilityComponent");

        HealthC = GetNode<HealthComponent>("HealthComponent");
        if (HealthC is not null)
        {
            HealthC.UnitStats = UnitStats;
        }
    }

    private void _setEnemyLogicAndAppearance()
    {
        if (Team == Global.UnitTeam.Enemy)
        {
            SelectableC?.QueueFree();

            var sprite = GetNode<Sprite2D>("Sprite2D");
            var atlasTexture = (AtlasTexture)sprite.Texture.Duplicate();
            sprite.Texture = atlasTexture;
            // Moves Atlas Region 16 pixel down without changing anything else
            // NOTE: needs to be changed when changing Textures!
            atlasTexture.Region = atlasTexture.Region with { Position = atlasTexture.Region.Position with { Y = 0 } };
        }
    }

    // --- LOGIC ---
    public bool Move(Vector2I targetTile)
    {
        Vector2I oldTile = TileLocation;
        TileLocation = targetTile;
        Position = Board.TileToWorldPosition(targetTile);

        if (!string.IsNullOrEmpty(OnMoveCallEventBus))
            EventBus.Instance.EmitSignal(OnMoveCallEventBus, this, oldTile, targetTile);
        EmitSignal(SignalName.UnitMoved, this, oldTile, targetTile);

        _log.ForContext("FromTile", oldTile)
            .ForContext("ToTile", targetTile)
            .Here()
            .Debug("Moved from {FromTile} to {ToTile}", oldTile, targetTile);

        return true;
    }
}