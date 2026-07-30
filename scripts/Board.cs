using AKidsDream.Globals;
using AKidsDream.resources.stateResources;
using AKidsDream.Units;
using Godot;
using Godot.Collections;
using CollectionExtensions = System.Collections.Generic.CollectionExtensions;

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
	/// TileMap layer used only for rendering the board visuals.
	/// Gameplay data is stored separately in <see cref="State"/>.
	/// </summary>
	[Export] public TileMapLayer Tilemap;

	/// <summary>
	/// Contains the logical board data:
	/// dimensions, tiles, units, and other gameplay information.
	/// </summary>
	public BoardState State = new();

	private Dictionary<int, Unit> _unitsById;


	// -- EDITOR TOOLS --
	/// <summary>
	/// Editor button to trigger board generation.
	/// </summary>
	[ExportToolButton("Generate Board")]
	private Callable GenerateBoardBtn => Callable.From(() =>
	{
		Tilemap.Scale = new Vector2(Global.TileMapScale, Global.TileMapScale);
		State = new BoardState();
		_generateBoard();
	});


	// -- LIFECYCLE --
	public void Init(BoardState boardState, Array<Unit> initialUnits)
	{
		State = boardState;
		
		Tilemap.Scale = new Vector2(Global.TileMapScale, Global.TileMapScale);
		_generateBoard();

		foreach (var newUnit in initialUnits) 
			AddUnit(newUnit);

		if (Engine.IsEditorHint()) return;
		
		EventBus.Instance.UnitCreated += OnUnitCreated;
		EventBus.Instance.UnitKilled += OnUnitKilled;
		EventBus.Instance.UnitMoved += OnUnitMoved;
		EventBus.Instance.EmitSignal(EventBus.SignalName.BoardGenerated);
	}


	// -- GENERATION --

	// TODO: Move Logic to a independent Loader/Saver Node, so it can save other things such as Utils.CurrentId, Round Number, each players mana and upgrades, and etc.
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
		GD.Print("Generating board...");

		// Starting atlas coordinate for the first tile.
		// X is alternated to create a checkerboard pattern.
		var atlasTile = Global.AtlasCoordsSprite.BeigeTile;


		for (var y = 0; y < State.Height; y++)
		{
			Array<TileData> row = [];

			for (var x = 0; x < State.Width; x++)
			{
				var tileLocation = new Vector2I(x, y);

				// Create logical tile representation.
				var tile = new TileData(tileLocation);
				row.Add(tile);


				// Alternate tile texture for checkerboard appearance.
				atlasTile = atlasTile == Global.AtlasCoordsSprite.DarkVioletTile
					? Global.AtlasCoordsSprite.BeigeTile
					: Global.AtlasCoordsSprite.DarkVioletTile;


				// Create visual representation in the TileMap.
				Tilemap.SetCell(
					tileLocation,
					0,
					Global.AtlasCoordsSpriteVectors[atlasTile]
				);
			}

			State.Tiles.Add(row);
		}

		GD.Print("Board generated");
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
	/// <param name="tileLocation">The tile coordinate where the unit should be placed.
	/// If null, extracts <see cref="tileLocation"/> from the <see cref="Unit"/>,
	/// else sets <see cref="Unit"/> tileLocation to <see cref="tileLocation"/>.</param>
	/// <remarks>
	/// <para>
	/// <strong>Warning:</strong> Recommended to not pass tileLocation,
	/// as this could cause desynchronization between the locations of the unit.
	/// </para>
	/// </remarks>
	public void AddUnit(Unit unit, Vector2I? tileLocation = null)
	{
		GD.Print($"Adding unit {unit.UnitId} to tile {tileLocation}");
		if (tileLocation is not null)
		{
			unit.TileLocation = tileLocation.Value;
		}

		TileData tile = State.Tiles[unit.TileLocation.Y][unit.TileLocation.X];
		tile.Unit = unit;
		_unitsById.Add(unit.UnitId, unit);
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

	public Unit? GetUnitById(int id)
	{
		return CollectionExtensions.GetValueOrDefault(_unitsById, id);
	}

	/// <summary>
	/// Gets the <see cref="Unit"/> at the specified tile location.
	/// </summary>
	/// <param name="location">The tile coordinate to check.</param>
	/// <returns>The <see cref="Unit"/> at the location, or null if out of bounds or no unit present.</returns>
	public Unit? GetUnitAt(Vector2I location)
	{
		return TileInBoard(location) ? State.Tiles[location.Y][location.X].Unit : null;
	}

	public TileData? GetTileAt(Vector2I location)
	{
		return TileInBoard(location) ? State.Tiles[location.Y][location.X] : null;
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
	public TileData? WorldPositionToTile(Vector2 worldPosition)
	{
		var tilePosition = WorldPositionToTilePosition(worldPosition);

		return TileInBoard(tilePosition) ? State.Tiles[tilePosition.Y][tilePosition.X] : null;
	}
	
	/// <summary>
	/// Returns the Tiles position <see cref="Vector2I"/> from the given World Position
	/// </summary>
	/// <param name="worldPosition"></param>
	/// <returns>Vector2I from the specified world position.</returns>
	public static Vector2I WorldPositionToTilePosition(Vector2 worldPosition)
	{
		var tilePosition = new Vector2I(
			(int)(worldPosition.X / Global.TileSize),
			(int)(worldPosition.Y / Global.TileSize)
		);

		return tilePosition;
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

	public Unit[] GetAllUnits()
	{
		return [.. _unitsById.Values];
	}
}
