using AKidsDream.Common.Logging;
using AKidsDream.Core.Managers;
using AKidsDream.Managers;
using AKidsDream.GameBoard;
using AKidsDream.Managers.SaveSystems;
using AKidsDream.Units.Resources.Components;
using AKidsDream.Utilities;
using Godot;
using Serilog;

namespace AKidsDream.Units.Resources;

/// <summary>
/// Base class for all unit types in the game.
/// Handles initialization and movement validation.
/// </summary>
[GlobalClass]
[Tool]
public partial class Unit : CharacterBody2D
{
    // -- PROPERTIES --
    public UnitId UnitId { get; private set; }

    [Export] public Global.UnitName UnitName;

    [Export] public int TeamIdInt;
    [Export] public int OwnerIdInt;
    public PlayerId OwnerId => new(OwnerIdInt);
    public TeamId TeamId => new(TeamIdInt);

    /// <summary>
    /// The current tile location of the unit.
    /// </summary>
    [Export] public Vector2I TileLocation;

    [Export] public UnitStatsData UnitStats;

    [Export] public StringName OnMoveCallEventBus;


    [Signal]
    public delegate void UnitMovedEventHandler(Unit unit, Vector2I from, Vector2I to);
    [Signal]
    public delegate void UnitInitializedEventHandler();

    [Export] public AnimatedSprite2D AnimationsPlayer;
    public HealthComponent HealthC { get; private set; }
    public SelectableComponent SelectableC { get; private set; }
    public DeathComponent DeathC { get; private set; }
    public AbilityComponent AbilityC { get; private set; }


    private ILogger _log = GameLogger.For<Unit>();
    public bool Initialized { get; private set; }

    public void Init(
        Global.UnitName unitName,
        PlayerId playerId,
        TeamId teamId,
        Vector2I tileLocation,
        UnitStatsData unitStats,
        UnitId? unitId = null
    )
    {
        var externalIdPassed = false;

        if (unitId is null)
            UnitId = UnitId.GetNextId();
        else
        {
            // externalId shouldn't be a bug... (LoadSaveSystem)
            externalIdPassed = true;
            UnitId = unitId.Value;
        }

        UnitName = unitName;
        OwnerIdInt = playerId.Value;
        TeamIdInt = teamId.Value;
        TileLocation = tileLocation;
        if (unitStats is not null) UnitStats = unitStats;

        AddToGroup(nameof(Global.Groups.Units));
        AddToGroup(teamId.ToString());

        Initialized = true;

        EmitSignal(SignalName.UnitInitialized);
        
        // If _Ready was already called before Init, call _Ready again to set appearance
        if (IsNodeReady())
        {
            Log.Error("Init() called after _Ready(). Recalling _Ready() to set appearance.");
            _Ready();
        }

        // Set static context for all future log calls
        _log = _log.ForContext("UnitId", UnitId)
            .ForContext("UnitName", UnitName)
            .ForContext("PlayerId", OwnerIdInt);

        if (externalIdPassed)
            _log.ForContext("TileLocation", TileLocation)
                .Here()
                .Warn(
                    "Unit Initialized With External Id at {TileLocation} with ID: {UnitId}",
                    TileLocation);

        EventBus.Instance.EmitSignal(EventBus.SignalName.UnitCreated, this);
    }

    // -- LOGIC --

    public override async void _Ready()
    {
        Position = Board.TileToWorldPosition(TileLocation);

        if (Engine.IsEditorHint()) return;

        if (!Initialized)
        {
            Log.Debug("_Ready() called before Init(). Waiting for initialization...");
            await ToSignal(this, SignalName.UnitInitialized);
            Log.Debug("Initialization complete, proceeding with _Ready() logic.");
        }

        EventBus.Instance.NewRoundStarted += OnNewRoundStarted;
        _injectReferenceAndAssignComponents();
        _setAppearance();
        _log.Here()
            .Debug("Unit ready at {TileLocation}", TileLocation);
    }

    public override void _ExitTree()
    {
        if (EventBus.Instance != null)
            EventBus.Instance.NewRoundStarted -= OnNewRoundStarted;
    }

    private void _injectReferenceAndAssignComponents()
    {
        DeathC = GetNode<DeathComponent>("DeathComponent");
        SelectableC = GetNode<SelectableComponent>("SelectableComponent");
        AbilityC = GetNode<AbilityComponent>("AbilityComponent");

        HealthC = GetNode<HealthComponent>("HealthComponent");
        HealthC.UnitStats = UnitStats;
    }

    private void _setAppearance()
    {
        GameManager.Instance.PlayerTeamRegistry.TryGetPlayer(OwnerId, out var ownerData);
        var animationName = AnimationsPlayer.GetAnimation().ToString().Replace(
            nameof(Global.UnitColor.Blue),
            ownerData.UnitColor.ToString());
        AnimationsPlayer.Play(animationName);
    }
    
    // -- Signal Handlers --
    private void OnNewRoundStarted(int playerIdInt, int round)
    {
        AbilityC.ResetPool();
    }

    // --- LOGIC ---
    public bool Move(Vector2I targetTile)
    {
        var oldTile = TileLocation;
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