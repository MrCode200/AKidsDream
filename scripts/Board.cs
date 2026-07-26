using System.IO;
using AKidsDream.Globals;
using AKidsDream.Units;
using Godot;
using Godot.Collections;

namespace AKidsDream.GameBoard;

/// <summary>
/// Handles the visual and data representation of the game board.
/// Generates the logical board state and creates the corresponding TileMap visuals.
/// </summary>
[Tool]
[GlobalClass]
public partial class Board : Node2D
{
	// -- REFERENCES --
	/// <summary>
	/// Name of the file containing the board state.
	/// </summary>
	[Export] public string StateFileName;

	/// <summary>
	/// If true, saves the board state when the board is removed from the scene tree.
	/// </summary>
	[Export] public bool SaveOnExit;

	/// <summary>
	/// TileMap layer used only for rendering the board visuals.
	/// Gameplay data is stored separately in <see cref="State"/>.
	/// </summary>
	[Export] public TileMapLayer Tilemap;

	/// <summary>
	/// Contains the logical board data:
	/// dimensions, tiles, units, and other gameplay information.
	/// </summary>
	public BoardState State;

	public static Board Instance { get; private set; }


	// -- EDITOR TOOLS --
	/// <summary>
	/// Editor button to trigger board generation.
	/// </summary>
	[ExportToolButton("Generate Board")]
	private Callable GenerateBoardBtn => Callable.From(_generateBoard);


	// -- LIFECYCLE --
	public override void _Ready()
	{
		Instance = this;
		_loadState();
		_generateBoard();

		EventBus.Instance.UnitCreated += OnUnitCreated;
		EventBus.Instance.UnitKilled += OnUnitKilled;
		EventBus.Instance.UnitMoved += OnUnitMoved;

		_initializeUnits();

		EventBus.Instance.EmitSignal(EventBus.SignalName.BoardGenerated);
	}

	public override void _ExitTree()
	{
		SaveState();
	}

	/// <summary>
	/// Loads the board state from file, or creates a new state if the file doesn't exist.
	/// </summary>
	private void _loadState()
	{
		State = ResourceIO.Load<BoardState>(Path.Combine(Global.SavePath, StateFileName)) ?? new BoardState();
	}

	/// <summary>
	/// Saves the current board state to file, including the initial unit positions.
	/// Only saves if SaveOnExit is enabled.
	/// </summary>
	public void SaveState()
	{
		if (!SaveOnExit)
			return;

		State.InitialUnits.Clear();

		// Iterate through BoardState tiles instead of scene tree
		foreach (var row in State.Tiles)
		{
			foreach (var tile in row)
			{
				if (tile.Unit != null)
				{
					State.InitialUnits[tile.TileLocation] = tile.Unit.Stats;
				}
			}
		}

		ResourceIO.Save(State, Path.Combine(Global.SavePath, StateFileName));
	}


	// -- GENERATION --

	/// <summary>
	/// <para>Creates the board grid based on the dimensions defined in <see cref="State"/>.</para>
	/// 
	/// <para><b>Generates: </b>
	/// <list type="number">
	/// <item>Logical TileData objects stored in BoardState.</item>
	/// <item>Visual tiles displayed through the TileMap.</item>
	/// </list>
	/// </para>
	/// </summary>
	private void _generateBoard()
	{
		Tilemap.Clear();
		State.Tiles.Clear();

		// Starting atlas coordinate for the first tile.
		// X is alternated to create a checkerboard pattern.
		Vector2I atlasCoords = new Vector2I(1, 12);


		for (int y = 0; y < State.Height; y++)
		{
			Array<TileData> row = new Array<TileData>();

			for (int x = 0; x < State.Width; x++)
			{
				Vector2I tileLocation = new Vector2I(x, y);

				// Create logical tile representation.
				TileData tile = new TileData(tileLocation);
				row.Add(tile);


				// Alternate tile texture for checkerboard appearance.
				atlasCoords.X = atlasCoords.X == 1 ? 2 : 1;


				// Create visual representation in the TileMap.
				Tilemap.SetCell(
					tileLocation,
					0,
					atlasCoords
				);
			}

			State.Tiles.Add(row);
		}

		GD.Print("Board generated");
	}

	/// <summary>
	/// Initializes units from the saved InitialUnits data in the board state.
	/// Loads unit scenes and places them at their saved positions.
	/// </summary>
	private void _initializeUnits()
	{
		foreach (var (location, data) in State.InitialUnits)
		{
			string scenePath = $"res://scenes/units/{data.UnitName.GetFieldValue<string>()}.tscn";
			PackedScene unitScene = GD.Load<PackedScene>(scenePath);

			Unit newUnit = unitScene.Instantiate<Unit>();
			newUnit.Stats = data;
			// Set position and TileLocation disway to skip signal emits from MoveC
			newUnit.Position = TileToWorldPosition(location);
			newUnit.TileLocation = location;

			GetNode<Node>("/root/GameWorld/EntityLayer").AddChild(newUnit);
		}
	}

	// -- Signal Handling --
	/// <summary>
	/// Handles the UnitCreated event by adding the unit to the board state.
	/// </summary>
	/// <param name="unit">The unit that was created.</param>
	private void OnUnitCreated(Unit unit) => AddUnit(unit, unit.TileLocation);

	/// <summary>
	/// Handles the UnitKilled event by removing the unit from the board state.
	/// </summary>
	/// <param name="unit">The unit that was killed.</param>
	private void OnUnitKilled(Unit unit) => RemoveUnit(unit.TileLocation);

	/// <summary>
	/// Handles the UnitMoved event by updating the unit's position in the board state.
	/// </summary>
	/// <param name="unit">The unit that moved.</param>
	/// <param name="oldTile">The previous tile location.</param>
	/// <param name="newTile">The new tile location.</param>
	private void OnUnitMoved(Unit unit, Vector2I oldTile, Vector2I newTile)
	{
		RemoveUnit(oldTile);
		AddUnit(unit, newTile);
	}

	// -- QUERIES --
	/// <summary>
	/// Adds a unit to the board state at the specified tile location.
	/// </summary>
	/// <param name="unit">The unit to add.</param>
	/// <param name="tileLocation">The tile coordinate where the unit should be placed.</param>
	public void AddUnit(Unit unit, Vector2I tileLocation)
	{
		GD.Print($"Adding unit {unit.Stats.UnitId} to tile {tileLocation}");
		TileData tile = State.Tiles[tileLocation.Y][tileLocation.X];
		tile.Unit = unit;
	}

	/// <summary>
	/// Removes a unit from the board state at the specified tile location.
	/// </summary>
	/// <param name="tileLocation">The tile coordinate where the unit should be removed.</param>
	public void RemoveUnit(Vector2I tileLocation)
	{
		GD.Print($"Removing unit from tile {tileLocation}");
		TileData tile = State.Tiles[tileLocation.Y][tileLocation.X];
		tile.Unit = null;
	}

	/// <summary>
	/// Gets the <see cref="Unit"/> at the specified tile location.
	/// </summary>
	/// <param name="location">The tile coordinate to check.</param>
	/// <returns>The <see cref="Unit"/> at the location, or null if out of bounds or no unit present.</returns>
	public Unit GetUnitAt(Vector2I location)
	{
		return TileInBoard(location) ? State.Tiles[location.Y][location.X].Unit : null;
	}

	/// <summary>
	/// Returns the <see cref="TileData"/> from the specified tile location.
	/// </summary>
	/// <param name="tile"></param>
	/// <returns>The <see cref="TileData"/> at the given Board Location></returns>
	public bool TileInBoard(Vector2I tile)
	{
		return tile.X >= 0 && tile.X < State.Width && tile.Y >= 0 && tile.Y < State.Height;
	}

	/// <summary>
	/// Returns the <see cref="TileData"/> from the specified world position.
	/// </summary>
	/// <param name="worldPosition">Vector2</param>
	/// <returns><see cref="TileData"/> from the specified world position, or null if out of bounds.</returns>
	public TileData WorldPositionToTile(Vector2 worldPosition)
	{
		Vector2I tilePosition = new Vector2I(
			(int)(worldPosition.X / Global.TileSize),
			(int)(worldPosition.Y / Global.TileSize)
		);

		return TileInBoard(tilePosition) ? State.Tiles[tilePosition.Y][tilePosition.X] : null;
	}

	/// <summary>
	/// Returns the center World Position of the given Tiles position 
	/// </summary>
	/// <param name="tilePosition">The position of the tile in tile coordinates <see cref="Vector2I"/>(0,0)</param>
	/// <returns></returns>
	public static Vector2 TileToWorldPosition(Vector2I tilePosition)
	{
		return tilePosition * Global.TileSize + new Vector2(Global.TileSize / 2, Global.TileSize / 2);
	}
}
