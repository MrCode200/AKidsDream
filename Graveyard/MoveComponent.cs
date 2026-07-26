/*
using Godot;
using System;
using System.Linq;
using AKidsDream.GameBoard;
using AKidsDream.Globals;
using AKidsDream.Units;
using AKidsDream.Units.FieldAccessPatterns;

namespace AKidsDream.Graveyard;

[GlobalClass]
public partial class MoveComponent : Node
{
    [Export] public Unit Unit;
    [Export] public AccessFieldPattern MovePattern;

    [Export] public StringName OnMoveCallEventBus;

    // -- SIGNALS --
    [Signal] public delegate void UnitMovedEventHandler(Unit unit, Vector2I from, Vector2I to);

    // -- MOVEMENT --
    /// <summary>
    /// Moves this Unit to the specified target tile if the move is valid.
    /// Updates the visual position after moving.
    /// </summary>
    /// <param name="targetTile">The destination tile coordinate.</param>
    /// <param name="skipValidation">To Skip Validation and Move the Unit to that tile (Overrides Unit on the TileData).</param>
    public bool Move(Vector2I targetTile, bool skipValidation = false)
    {
        if (!skipValidation)
        {
            if (!ValidateMove(targetTile)) return false;
        }

        Vector2I oldTile = Unit.TileLocation;
        Unit.TileLocation = targetTile;
        Unit.Position = Board.TileToWorldPosition(targetTile);

        if (!string.IsNullOrEmpty(OnMoveCallEventBus))
            EventBus.Instance.EmitSignal(OnMoveCallEventBus, Unit, oldTile, targetTile);
        EmitSignal(SignalName.UnitMoved, Unit, oldTile, targetTile);

        GD.Print($"Moved from {Unit.TileLocation} to {targetTile}");

        return true;
    }

    // -- VALIDATION --
    public Vector2I[] ValidMoves()
    {
        if (MovePattern == null)
        {
            GD.PrintErr("MovePattern: No pattern configured!");
            return Array.Empty<Vector2I>();
        }

        return MovePattern.GetTiles(Unit.TileLocation, Board.Instance);
    }

    /// <summary>
    /// Checks if moving to the target tile is valid.
    /// </summary>
    /// <param name="targetTile">The tile to validate.</param>
    /// <returns>True if the tile is in valid moves or attacks, false otherwise.</returns>
    public bool ValidateMove(Vector2I targetTile)
    {
        return ValidMoves().Contains(targetTile);
    }

}
*/

