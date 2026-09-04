using AKidsDream.Abilities;
using AKidsDream.Common.Components.TweenComponent.Resources;
using AKidsDream.Common.Logging;
using AKidsDream.Util.Identifiers;
using AKidsDream.Core.Teams;
using AKidsDream.GameBoard;
using AKidsDream.Managers.SaveSystems;
using Godot;
using Serilog;

namespace AKidsDream.Common;

/// <summary>
/// Base class for all unit types in the game.
/// Handles initialization and movement validation.
/// </summary>
[GlobalClass]
[Tool]
public partial class Unit : CharacterBody2D, IAbilityCaster
{
    // -- PROPERTIES --
    public UnitId UnitId { get; private set; }
    public IIdTag CasterId => UnitId; // IAbilityCaster interface
    public string CasterName => UnitName.ToString(); // IAbilityCaster interface

    [Export] public Global.UnitName UnitName;

    [Export] public int TeamIdInt;
    [Export] public int OwnerIdInt;

    public PlayerId OwnerId => new(OwnerIdInt);
    public TeamId TeamId => new(TeamIdInt);

    /// <summary>
    /// The current tile location of the unit.
    /// </summary>
    [Export] public Vector2I TileLocation {get; set;}

    [Export] public UnitStatsData UnitStats;

    [Signal]
    public delegate void UnitMovedEventHandler(Unit unit, Vector2I from, Vector2I to);
    [Signal]
    public delegate void UnitInitializedEventHandler();

    /// <summary>
    /// The components of the unit.
    /// </summary>
    public HealthComponent HealthComp { get; private set; }
    public SelectableComponent SelectableComp { get; private set; }
    public DeathComponent DeathC { get; private set; }
    public AbilityComponent AbilityC { get; private set; }
    public AnimationComponent AnimComp { get; private set; }
    private Board Board { get; set; }


    private ILogger _log = GameLogger.For<Unit>();
    public bool Initialized { get; private set; }

    
    public override async void _Ready()
    {
        if (Engine.IsEditorHint()) return;

        if (!Initialized)
        {
            await ToSignal(this, SignalName.UnitInitialized);
        }

        AnimComp!.PlayAnimation(AnimComp.DefaultAnimation);
    }
    
    public override void _ExitTree()
    {
        if (EventBus.Instance != null)
            EventBus.Instance.TurnStarted -= OnTurnStarted;
    }

    // -- LOGIC --

    public void Init(
        PlayerData ownerPlayerData,
        Vector2I tileLocation,
        UnitStatsData unitStats,
        Board board,
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

        Board = board;
        
        UnitName = unitStats.UnitName;
        OwnerIdInt = ownerPlayerData.PlayerId.Value;
        TeamIdInt = ownerPlayerData.TeamId.Value;
        TileLocation = tileLocation;
        Position = Board.TileToWorldPosition(TileLocation);

        UnitStats = (UnitStatsData)unitStats.Duplicate();

        AddToGroup(nameof(Global.Groups.Units));
        AddToGroup(ownerPlayerData.TeamId.ToString());
        
        EventBus.Instance.TurnStarted += OnTurnStarted;
        _initializeDependencies(ownerPlayerData.UnitColor);

        Initialized = true;
        EmitSignal(SignalName.UnitInitialized);

        // Set static context for all future log calls
        _log = _log.ForContext("IdTag", UnitId)
            .ForContext("NameTag", UnitName)
            .ForContext("PlayerId", OwnerIdInt);

        if (externalIdPassed)
            _log.ForContext("TileLocation", TileLocation)
                .Here()
                .Warn(
                    "Unit Initialized With External Id at {TileLocation} with ID: {UnitId}",
                    TileLocation);

        EventBus.Instance.EmitSignal(EventBus.SignalName.UnitCreated, this);
    }

    private void _initializeDependencies(Global.UnitColor unitColor)
    {
        DeathC = GetNode<DeathComponent>("DeathComponent");
        SelectableComp = GetNode<SelectableComponent>("SelectableComponent");
        AbilityC = GetNode<AbilityComponent>("AbilityComponent");
        
        AnimComp = GetNode<AnimationComponent>("AnimationComponent");
        AnimComp!.Init(this, unitColor);

        HealthComp = GetNode<HealthComponent>("HealthComponent");
        HealthComp.UnitStats = UnitStats;
    }
    
    // -- Signal Handlers --
    private void OnTurnStarted(int playerIdInt, int round)
    {
        if (OwnerIdInt == playerIdInt)
        {
            AbilityC.ResetPool();
        }
    }

    // --- LOGIC ---
    public bool Move(Vector2I toTile)
    {
        if (Board is null)
        {
            _log.Here().Err("Board not found in group '{GroupName}'", nameof(Global.Groups.Board));
            return false;
        }        
        
        if (!Board.MoveUnit(this, toTile, out var position))
            return false;

        Position = position;
        TileLocation = toTile;

        return true;
    }
}