using AKidsDream.Components;
using AKidsDream.Scripts;
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
	[Export] public Utils.UnitTeam Team = Utils.UnitTeam.Player;
	[Export] public StatsData Stats;
	[Export] public Vector2I SpawnTileLocation;
	public MoveComponent MoveC;

	
	// -- LOGIC --
	public override void _Ready()
	{
		_getComponents();
		_initAndInjectStatsResource();
		_setSpawnLocation();
		_setAppearance();

		EventBus.Instance.EmitSignal(EventBus.SignalName.UnitCreated, this);
		GD.Print($"Unit ready at {MoveC.TileLocation}; Team: {Stats.Team}");
	}
	
	private void _getComponents()
	{
		MoveC = GetNode<MoveComponent>("MoveComponent");
	}

	private void _initAndInjectStatsResource()
	{
		Stats = (StatsData)Stats.Duplicate();
		Stats.Team = Team;
		
		var attackComponent = GetNode<AttackComponent>("AttackComponent");
		if (attackComponent != null) attackComponent.Stats = Stats;
		
		var healthComponent = GetNode<HealthComponent>("HealthComponent");
		if (healthComponent != null) healthComponent.Stats = Stats;
	}
	
	private void _setSpawnLocation()
	{
		GD.Print($"Setting spawn location for {Stats.UnitId} at {SpawnTileLocation}");
		MoveC.Move(SpawnTileLocation, true);
	}
	
	private void _setAppearance()
	{
		if (Stats.Team == Utils.UnitTeam.Enemy)
		{
			var selectable = GetNode<SelectableComponent>("SelectableComponent");
			selectable?.QueueFree();
			
			var sprite = GetNode<Sprite2D>("Sprite2D");
			var atlasTexture  = (AtlasTexture)sprite.Texture.Duplicate();
			sprite.Texture = atlasTexture;
			// Moves Atlas Region 16 pixel down without changing anything else
			// NOTE: needs to be changed when changing Textures!
			atlasTexture.Region = atlasTexture.Region with { Position = atlasTexture.Region.Position with { Y = 0 } };        
		}
	}
}
