using System;
using System.Linq;
using AKidsDream.GameBoard;
using AKidsDream.Globals;
using Godot;
using AKidsDream.Units;
using AKidsDream.Units.FieldAccessPatterns;
using Godot.Collections;

namespace AKidsDream.Abilities;

[GlobalClass]
public abstract partial class EffectData : Resource
{
    [Export] public AccessFieldPattern EffectPattern;
    [Export] public Global.AtlasCoordsSprite EffectAtlasCoords;

    /// <summary>
    /// The minimum number of Tiles the User needs to select.
    /// </summary>
    private int _minTargets = 1;
    [Export] public int MinTargets
    {
        get => _minTargets;
        set
        {
            _minTargets = value;
            if (_minTargets > MaxTargets)
                MaxTargets = _minTargets;
        }
    }
    /// <summary>
    /// The maximum number of Tiles the User needs to select.
    /// </summary>
    private int _maxTargets = 1;
    [Export] public int MaxTargets
    {
        get => _maxTargets;
        set
        {
            _maxTargets = value;
            if (_maxTargets < MinTargets)
                MinTargets = _maxTargets;
        }
    }
    
    /// <summary>
    /// Whether the User can select the same Tile multiple times.
    /// </summary>
    [Export] public bool AllowDuplicateTiles;
    
    /// <summary>
    /// Returns the Tiles that will be affected by the effect.
    /// </summary>
    /// <param name="targetTiles">The tiles the player has selected</param>
    /// <param name="board">The board containing TileData's</param>
    /// <returns>An array of <see cref="Vector2I"/> which is the TileData.TileLocation</returns>
    protected Vector2I[] GetAffectedTiles(Vector2I[] targetTiles, Board board)
    {
        if (EffectPattern == null)
        {
            GD.PrintErr("EffectPattern is null");
            return [];
        }

        return targetTiles
            .SelectMany(tile => EffectPattern.GetTiles(tile, board))
            .ToArray();
    }

    /// <summary>
    /// Returns the atlas coordinates and tiles that will be used to visualize the effect.
    /// </summary>
    /// <param name="source">The <see cref="Unit"/> who the Ability belongs to</param>
    /// <param name="board">The <see cref="Board"/></param>
    /// <param name="targetTiles">An array of <see cref="Vector2I"/> representing the selected Tiles</param>
    public (Vector2I atlasCoord, Vector2I[] tiles) GetEffectVisualizationData(
        Unit source,
        Board board,
        Vector2I[] targetTiles
    )
    {
        // TODO: Handle visualization of duplicate tiles
        var tiles = GetAffectedTiles(targetTiles, board);
        return (Global.AtlasCoordsSpriteVectors[EffectAtlasCoords], tiles);
    }

    /// <summary>
    /// Checks if the number of Tiles the User selected is valid.
    /// If AllowDuplicateTiles is false, all Tiles must be unique.
    /// Calls <see cref="ApplyEffect"/> if the number of Tiles is valid.
    /// </summary>
    /// <param name="source">The <see cref="Unit"/> who the Ability belongs to</param>
    /// <param name="board">The <see cref="Board"/></param>
    /// <param name="targetTiles">An array of <see cref="Vector2I"/> representing the selected Tiles</param>
    /// <returns>Returns an <see cref="EffectResult"/> which contains data of what effect did what.</returns>
    public EffectResult Apply(Unit source, Board board, Vector2I[] targetTiles)
    {
        if (HasValidTargetCount(targetTiles)) return ApplyEffect(source, board, targetTiles);

        GD.PrintErr($"Invalid amount of targets: {targetTiles.Length}");
        return new InvalidTargetCountErrorResult()
        {
            Source = source, Effect = this, Error = $"Invalid amount of targets: {targetTiles.Length}",
            Actual = targetTiles.Length
        };
    }

    /// <summary>
    /// Checks if the number of Tiles the User selected is valid.
    /// If AllowDuplicateTiles is false, all Tiles must be unique.
    /// </summary>
    /// <param name="targetTiles">The Tiles the User selected.</param>
    public bool HasValidTargetCount(Vector2I[] targetTiles)
    {
        var count = targetTiles.Length;
        if (count < MinTargets || count > MaxTargets) return false;
        if (!AllowDuplicateTiles && targetTiles.Distinct().Count() != count) return false;

        return true;
    }

    public abstract EffectResult ApplyEffect(Unit source, Board board, Vector2I[] targetTiles);
}