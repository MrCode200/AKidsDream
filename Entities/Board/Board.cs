#nullable enable
using System.Diagnostics.CodeAnalysis;
using AKidsDream.Common.Logging;
using AKidsDream.Managers.SaveSystems;
using AKidsDream.Managers.SaveSystem.Resources;
using AKidsDream.Units.Resources;
using Godot;
using Godot.Collections;
using Serilog;
using TileData = AKidsDream.Managers.SaveSystem.Resources.TileData;

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
    /// Gameplay data is stored separately in <see cref="StateData"/>.
    /// </summary>
    [Export] public TileMapLayer Tilemap;

    /// <summary>
    /// Contains the logical board data:
    /// dimensions, tiles, units, and other gameplay information.
    /// </summary>
    public BoardStateData StateData = new();

    private ILogger _log = GameLogger.For<Board>();

    private readonly System.Collections.Generic.Dictionary<UnitId, Unit> _unitsById = new();


    // -- EDITOR TOOLS --
    /// <summary>
    /// Editor button to trigger board generation.
    /// </summary>
    [ExportToolButton("Generate Board")]
    private Callable GenerateBoardBtn => Callable.From(() =>
    {
        Tilemap.Scale = new Vector2(Global.TileMapScale, Global.TileMapScale);
        StateData = new BoardStateData();
        _generateBoard();
    });


    // -- LIFECYCLE --
    public void Init(BoardStateData boardStateData, Array<Unit>? initialUnits = null)
    {
        StateData = boardStateData;

        _log.Here().Info(
            "Initializing board '{BoardName}' with {Width}x{Height} tiles and {InitialUnitCount} initial units",
            Name,
            StateData.Width,
            StateData.Height,
            initialUnits?.Count ?? 0);

        Tilemap.Scale = new Vector2(Global.TileMapScale, Global.TileMapScale);
        _generateBoard();

        foreach (var newUnit in initialUnits ?? [])
            AddUnit(newUnit);

        if (Engine.IsEditorHint()) return;

        EventBus.Instance.UnitCreated += OnUnitCreated;
        EventBus.Instance.UnitKilled += OnUnitKilled;
        EventBus.Instance.UnitMoved += OnUnitMoved;

        _log.Here().Info(
            "Board '{BoardName}' initialized with {UnitCount} Units",
            Name,
            _unitsById.Count);
    }


    // -- GENERATION --

    /// <summary>
    /// <para>Creates the board grid based on the dimensions defined in <see cref="StateData"/>.</para>
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
        StateData.Tiles.Clear();
        _log.Here().Info(
            "Generating board '{BoardName}' {Width}x{Height}; TilemapPath: {TileMapPath}",
            Name,
            StateData.Width,
            StateData.Height,
            Tilemap?.GetPath().ToString());

        // Starting atlas coordinate for the first tile.
        // X is alternated to create a checkerboard pattern.
        var atlasTile = Global.AtlasCoordsSprite.BeigeTile;


        for (var y = 0; y < StateData.Height; y++)
        {
            Array<TileData> row = [];
            
            // If width is even, alternate starting tile for checkerboard pattern.
            if (StateData.Width % 2 == 0)
                atlasTile = atlasTile == Global.AtlasCoordsSprite.DarkVioletTile
                    ? Global.AtlasCoordsSprite.BeigeTile
                    : Global.AtlasCoordsSprite.DarkVioletTile;
                
            for (var x = 0; x < StateData.Width; x++)
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
                Tilemap!.SetCell(
                    tileLocation,
                    0,
                    Global.AtlasCoordsSpriteVectors[atlasTile]
                );
            }

            StateData.Tiles.Add(row);
        }

        _log.Here().Info(
            "Board generated '{BoardName}' {Width}x{Height} with {TileCount} Tiles",
            Name,
            StateData.Width,
            StateData.Height,
            StateData.Width * StateData.Height);
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
        if (tileLocation is not null)
        {
            unit.TileLocation = tileLocation.Value;
        }
        
        TileData tile = StateData.Tiles[unit.TileLocation.Y][unit.TileLocation.X];
        tile.Unit = unit;

        // Check for duplicate unit id, if found, replace old registration
        if (_unitsById.TryGetValue(unit.UnitId, out var oldUnit))
        {
            _log.Here().Warn(
                "Duplicate unit id detected while adding unit to board; " +
                "replacing existing registration '{OldUnitName}' (id: {OldUnitId}) at {OldTileLocation} " +
                "with '{UnitName}' (id: {UnitId}) at {TileLocation} in Board '{BoardName}'",
                oldUnit.UnitName,
                oldUnit.UnitId,
                oldUnit.TileLocation,
                unit.UnitName,
                unit.UnitId,
                unit.TileLocation,
                Name);
            _unitsById.Remove(unit.UnitId);
        }

        _unitsById.Add(unit.UnitId, unit);
        _log.Here().Debug(
            "Added unit '{UnitName}' (id: {UnitId}) at {TileLocation} to board '{BoardName}'",
            unit.UnitName,
            unit.UnitId,
            unit.TileLocation,
            Name
        );
    }

    /// <summary>
    /// Removes a unit from the board state at the specified tile location.
    /// </summary>
    /// <param name="tileLocation">The tile coordinate where the unit should be removed.</param>
    public void RemoveUnit(Vector2I tileLocation)
    {
        TileData tile = StateData.Tiles[tileLocation.Y][tileLocation.X];
        var removedUnit = tile.Unit;

        _unitsById.Remove(removedUnit?.UnitId ?? UnitId.None);
        tile.Unit = null;

        _log.Here().Debug(
            "Removed unit '{UnitName}' (id: {UnitId}) from {TileLocation} in board '{BoardName}'",
            removedUnit?.UnitName,
            removedUnit?.UnitId,
            tileLocation,
            Name);
    }

    /// <summary>
    /// Tries to get the <see cref="Unit"/> with the specified identifier.
    /// </summary>
    /// <param name="id">The identifier of the unit to look up.</param>
    /// <param name="unit">
    /// When this method returns <c>true</c>, contains the unit with the specified identifier;
    /// otherwise, <c>null</c>.
    /// </param>
    /// <returns>
    /// <c>true</c> if a unit with the given identifier exists; otherwise, <c>false</c>.
    /// </returns>
    public bool TryGetUnitById(UnitId id, [NotNullWhen(true)] out Unit? unit)
    {
        return _unitsById.TryGetValue(id, out unit);
    }

    /// <summary>
    /// Tries to get the <see cref="Unit"/> at the specified tile location.
    /// </summary>
    /// <param name="location">The tile coordinate to check.</param>
    /// <param name="unit">
    /// When this method returns, contains the unit at the specified location,
    /// or <c>null</c> if the location is out of bounds or has no unit.
    /// </param>
    /// <returns><c>true</c> if a unit exists at the location; otherwise, <c>false</c>.</returns>
    public bool TryGetUnitAt(Vector2I location, [NotNullWhen(true)] out Unit? unit)
    {
        unit = null;
        if (!TileInBoard(location)) return false;

        unit = StateData.Tiles[location.Y][location.X].Unit;
        return true;
    }

    /// <summary>
    /// Tries to get the <see cref="TileData"/> at the specified tile location.
    /// </summary>
    /// <param name="location">The tile coordinate to check.</param>
    /// <param name="tile">
    /// When this method returns <c>true</c>, contains the tile data at the specified location;
    /// otherwise, <c>null</c>.
    /// </param>
    /// <returns><c>true</c> if the location is within the board bounds; otherwise, <c>false</c>.</returns>
    public bool TryGetTileAt(Vector2I location, [NotNullWhen(true)] out TileData? tile)
    {
        tile = null;
        if (!TileInBoard(location)) return false;

        tile = StateData.Tiles[location.Y][location.X];
        return true;
    }

    /// <summary>
    /// Determines whether the specified tile location is within the board bounds.
    /// </summary>
    /// <param name="tileLocation">The tile coordinate to check.</param>
    /// <returns><c>true</c> if the location is inside the board; otherwise, <c>false</c>.</returns>
    public bool TileInBoard(Vector2I tileLocation)
    {
        return tileLocation.X >= 0 &&
               tileLocation.X < StateData.Width &&
               tileLocation.Y >= 0 &&
               tileLocation.Y < StateData.Height;
    }

    /// <summary>
    /// Returns the <see cref="TileData"/> from the specified world position.
    /// </summary>
    /// <param name="worldPosition">Vector2</param>
    /// <returns><see cref="TileData"/> from the specified world position, or null if out of bounds.</returns>
    public TileData? WorldPositionToTile(Vector2 worldPosition)
    {
        var tilePosition = WorldPositionToTilePosition(worldPosition);

        return TileInBoard(tilePosition) ? StateData.Tiles[tilePosition.Y][tilePosition.X] : null;
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