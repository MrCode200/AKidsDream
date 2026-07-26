using AKidsDream.Components;
using AKidsDream.GameBoard;
using AKidsDream.Globals;
using Godot;

namespace AKidsDream.Units;

/// <summary>
/// Base class for all unit types in the game.
/// Handles initialization & movement validation.
/// </summary>
[GlobalClass]
public partial class Unit : CharacterBody2D
{
    // -- PROPERTIES --
    [Export] public StatsData Stats;

    /// <summary>
    /// The current tile location of the unit.
    /// </summary>
    [Export] public Vector2I TileLocation;

    [Export] public StringName OnMoveCallEventBus;

    [Signal]
    public delegate void UnitMovedEventHandler(Unit unit, Vector2I from, Vector2I to);


    public AttackComponent AttackC { get; private set; }
    public HealthComponent HealthC { get; private set; }
    public SelectableComponent SelectableC { get; private set; }
    public DeathComponent DeathC { get; private set; }
    public AbilityComponent AbilityC { get; private set; }

    public Unit()
    {
    }

    public Unit(StatsData stats)
    {
        Stats = stats;
    }

    // -- LOGIC --

    public override void _Ready()
    {
        _injectReferenceAndAssignComponents();
        _setAppearance();

        AddToGroup(Global.Groups.Units.GetFieldValue<string>());
        AddToGroup((Stats.Team == Global.UnitTeam.Enemy)
            ? Global.Groups.EnemyUnits.GetFieldValue<string>()
            : Global.Groups.PlayerUnits.GetFieldValue<string>()
        );
        EventBus.Instance.EmitSignal(EventBus.SignalName.UnitCreated, this);
        GD.Print($"Unit ready at {TileLocation}; Team: {Stats.Team}");
    }

    private void _injectReferenceAndAssignComponents()
    {
        DeathC = GetNode<DeathComponent>("DeathComponent");
        SelectableC = GetNode<SelectableComponent>("SelectableComponent");
        AttackC = GetNode<AttackComponent>("AttackComponent");
        AbilityC = GetNode<AbilityComponent>("AbilityComponent");

        HealthC = GetNode<HealthComponent>("HealthComponent");
        if (HealthC is not null)
        {
            HealthC.Stats = Stats;
        }
    }

    private void _setAppearance()
    {
        if (Stats.Team == Global.UnitTeam.Enemy)
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

        GD.Print($"Moved from {TileLocation} to {targetTile}");

        return true;
    }
}