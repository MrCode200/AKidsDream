using Godot;
using System;
using System.Linq;
using AKidsDream.GameBoard;
using AKidsDream.Scripts;

namespace AKidsDream.Components;

[GlobalClass]
public partial class MoveComponent : Node
{
	[Export] public Node2D Body;

	[Export] public StringName OnMoveCallEventBus;

	/// <summary>
	/// The tile location of this Unit on the board.
	/// </summary>
	[Export] public Vector2I TileLocation;
	
	// -- SIGNALS --
	[Signal] public delegate void BodyMovedEventHandler(Node2D body, Vector2I from, Vector2I to);
	
	// -- LOGIC --
	public override void _Ready()
	{
		Body.Position = Board.TileToWorld(TileLocation);
	}
	
	// -- MOVEMENT --
	/// <summary>
	/// Moves this Body to the specified target tile if the move is valid.
	/// Updates the visual position after moving.
	/// </summary>
	/// <param name="targetTile">The destination tile coordinate.</param>
	/// <param name="skipValidation">To Skip Validation and Move the Body to that tile (Overrides Body on the TileData).</param>
	public void Move(Vector2I targetTile, bool skipValidation = false)
	{
		if (!skipValidation)
		{
			if (!ValidateMove(targetTile)) return;
		}

		Vector2I oldTile = TileLocation;
		TileLocation = targetTile;
		Body.Position = Board.TileToWorld(targetTile);
		
		if (!string.IsNullOrEmpty(OnMoveCallEventBus)) 
			EventBus.Instance.EmitSignal(OnMoveCallEventBus, Body, oldTile, targetTile);
		EmitSignal(SignalName.BodyMoved, Body, oldTile, targetTile);
		
		GD.Print($"Moved from {TileLocation} to {targetTile}");
	}

	// -- VALIDATION --
	/// <summary>
	/// Checks if moving to the target tile is valid.
	/// </summary>
	/// <param name="targetTile">The tile to validate.</param>
	/// <returns>True if the tile is in valid moves or attacks, false otherwise.</returns>
	public bool ValidateMove(Vector2I targetTile)
	{
		return ValidTiles().Contains(targetTile);
	}

	/// <summary>
	/// Gets all valid tiles this Body can move to or attack.
	/// </summary>
	/// <returns>Combined array of valid moves and valid attack targets.</returns>
	public Vector2I[] ValidTiles()
	{
		return ValidMoves().Concat(ValidAttacks()).ToArray();
	}

	// -- VIRTUAL METHODS --
	/// <summary>
	/// Calculates the tiles this Body can move to.
	/// Must be overridden by derived classes.
	/// </summary>
	/// <returns>Array of valid move target tiles.</returns>
	public virtual Vector2I[] ValidMoves()
	{
		throw new NotImplementedException("ValidMoves function not implemented");;
	}

	/// <summary>
	/// Calculates the tiles this Body can attack.
	/// Must be overridden by derived classes.
	/// </summary>
	/// <returns>Array of valid attack target tiles.</returns>
	public virtual Vector2I[] ValidAttacks()
	{
		throw new NotImplementedException("ValidAttacks function not implemented");
	}
}
