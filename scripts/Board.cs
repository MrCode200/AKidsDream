using AKidsDream.Scripts;
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
	/// Contains the logical board data:
	/// dimensions, tiles, units, and other gameplay information.
	/// </summary>
	[Export] public BoardState State;

	/// <summary>
	/// TileMap layer used only for rendering the board visuals.
	/// Gameplay data is stored separately in <see cref="State"/>.
	/// </summary>
	[Export] public TileMapLayer Tilemap;

	public static Board Instance { get; private set; }


	// -- EDITOR TOOLS --
	[ExportToolButton("Generate Board")] public Callable GenerateBoardBtn => Callable.From(GenerateBoard);


	// -- LIFECYCLE --
	public override void _Ready()
	{
		Instance = this;
		GenerateBoard();

		EventBus.Instance.UnitCreated += OnUnitCreated;
		EventBus.Instance.UnitKilled += OnUnitKilled;
		EventBus.Instance.UnitMoved += OnUnitMoved;
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
	private void GenerateBoard()
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
	
	// -- Signal Handling --
	private void OnUnitCreated(Unit unit) => AddUnit(unit, unit.MoveC.TileLocation);
	private void OnUnitKilled(Unit unit) => RemoveUnit(unit.MoveC.TileLocation);

	private void OnUnitMoved(Unit unit, Vector2I oldTile, Vector2I newTile)
	{
		RemoveUnit(oldTile);
		AddUnit(unit, newTile);
	}
	
	// -- QUERIES --
	public void AddUnit(Unit unit, Vector2I tileLocation)
	{
		GD.Print($"Adding unit {unit.Stats.UnitId} to tile {tileLocation}");
		TileData tile = State.Tiles[tileLocation.Y][tileLocation.X];
		tile.Unit = unit;
	}
	
	public void RemoveUnit(Vector2I tileLocation)
	{
		GD.Print($"Removing unit from tile {tileLocation}");
		TileData tile = State.Tiles[tileLocation.Y][tileLocation.X];
		tile.Unit = null;
	}
	
	/// <summary>
	/// Gets the <see cref="UnitMovement"/> at the specified tile location.
	/// </summary>
	/// <param name="location">The tile coordinate to check.</param>
	/// <returns>The <see cref="UnitMovement"/> at the location, or null if out of bounds or no unit present.</returns>
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
	public static Vector2 TileToWorld(Vector2I tilePosition)
	{
		return tilePosition * Global.TileSize + new Vector2(Global.TileSize / 2, Global.TileSize / 2);
	}

}
