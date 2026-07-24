using AKidsDream.Components;
using AKidsDream.Globals;
using Godot;

namespace AKidsDream.Units;

/// <summary>
/// Base class for all unit types in the game.
/// Handles unit positioning, movement validation, and attack/move range calculation.
/// Derived classes should override <see cref="ValidMoves"/> and <see cref="ValidAttacks"/>.
/// </summary>
[GlobalClass]
public partial class Unit : CharacterBody2D
{
	// -- PROPERTIES --
	/// <summary>
	/// The <see cref="Utils.UnitTeam"/> this unit belongs to (Player or Enemy).
	/// Used for determining valid attack targets.
	/// </summary>
	[Export] public StatsData Stats;
	
	/// <summary>
	/// The current tile location of the unit.
	/// </summary>
	[Export] public Vector2I TileLocation;
	
	public MoveComponent MoveC { get; private set; }
	public AttackComponent AttackC { get; private set; }
	public HealthComponent HealthC { get; private set; }
	public SelectableComponent SelectableC { get; private set; }
	public DeathComponent DeathC { get; private set; }
	public ActionComponent ActionC { get; private set; }

	public Unit() { }
	
	public Unit(StatsData stats)
	{
		Stats = stats;
	}
	
	// -- LOGIC --

	public override void _Ready()
	{
		_injectReferenceAndAssignComponents();
		_setAppearance();

		AddToGroup(Global.Groups.Units.GetFieldStringValue());
		AddToGroup((Stats.Team == Global.UnitTeam.Enemy) ? 
			Global.Groups.EnemyUnits.GetFieldStringValue() : Global.Groups.PlayerUnits.GetFieldStringValue()
		);
		EventBus.Instance.EmitSignal(EventBus.SignalName.UnitCreated, this);
		GD.Print($"Unit ready at {TileLocation}; Team: {Stats.Team}");
	}

	private void _injectReferenceAndAssignComponents()
	{
		AttackC = GetNode<AttackComponent>("AttackComponent");
		if (AttackC != null)
		{
			AttackC.Unit = this;
		};
		
		HealthC = GetNode<HealthComponent>("HealthComponent");
		if (HealthC != null)
		{
			HealthC.Stats = Stats;
		};
		
		var raw = GetNode("MoveComponent"); // untyped
		GD.Print($"Class: {raw.GetClass()}, Script: {raw.GetScript().AsGodotObject()?.GetType().FullName}");		MoveC = GetNode<MoveComponent>("MoveComponent");
		if (MoveC != null)
		{
			MoveC.Unit = this;
		};

		ActionC = GetNode<ActionComponent>("ActionComponent");
		if (ActionC != null)
		{
			ActionC.MoveC = MoveC;
			ActionC.AttackC = AttackC;
			ActionC.SelectC = SelectableC;
		}
		
		DeathC = GetNode<DeathComponent>("DeathComponent");
		SelectableC = GetNode<SelectableComponent>("SelectableComponent");
	}

	private void _setAppearance()
	{
		if (Stats.Team == Global.UnitTeam.Enemy)
		{
			SelectableC?.QueueFree();
			
			var sprite = GetNode<Sprite2D>("Sprite2D");
			var atlasTexture  = (AtlasTexture)sprite.Texture.Duplicate();
			sprite.Texture = atlasTexture;
			// Moves Atlas Region 16 pixel down without changing anything else
			// NOTE: needs to be changed when changing Textures!
			atlasTexture.Region = atlasTexture.Region with { Position = atlasTexture.Region.Position with { Y = 0 } };        
		}
	}
}

