using Godot;
using System;
using System.Linq;
using AKidsDream.GameBoard;
using AKidsDream.Units;
using AKidsDream.Units.FieldAccessPatterns;

namespace AKidsDream.Components;

/// <summary>
/// Handles attack logic for a unit.
/// Uses configurable AttackPattern to determine valid attack targets.
/// </summary>
[GlobalClass]
public partial class AttackComponent : Node
{
    [Export] public Unit Unit;

    /// <summary>
    /// Event bus signal for unit movement.
    /// </summary>
    [Signal]
    public delegate void UnitAttackedEventHandler(Unit unit, Unit target);


    /// <summary>
    /// The attack pattern that determines valid attack targets.
    /// Configure this in the editor with MeleePattern, RangedPattern, or custom patterns.
    /// </summary>
    [Export] public AccessFieldPattern AttackPattern;

    // -- LOGIC --

    /// <summary>
    /// Gets all valid attack target tiles based on the configured pattern.
    /// </summary>
    /// <returns>Array of valid attack target tile coordinates.</returns>
    public Vector2I[] ValidAttacks()
    {
        if (AttackPattern == null)
        {
            GD.PrintErr("AttackComponent: No pattern configured!");
            return Array.Empty<Vector2I>();
        }

        return AttackPattern.GetTiles(Unit.TileLocation, Board.Instance, Unit.Stats);
    }

    // -- VALIDATION --

    /// <summary>
    /// Validates if the target tile is a valid attack target.
    /// </summary>
    /// <param name="targetTile">The tile to validate.</param>
    /// <returns>True if the tile is a valid attack target, false otherwise.</returns>
    public bool ValidateAttack(Vector2I targetTile)
    {
        return ValidAttacks().Contains(targetTile);
    }

    /// <summary>
    /// Performs an attack on the target unit.
    /// </summary>
    /// <param name="target">The unit to attack.</param>
    /// <param name="skipValidation">Skips validation if set to true.</param>
    /// <returns>True if the attack was successful, false otherwise.</returns>
    public bool Attack(Unit target, bool skipValidation = false)
    {
        if (!skipValidation)
        {
            if (!ValidateAttack(target.TileLocation)) return false;
        }

        target.HealthC?.Damage(Unit.Stats.Attack);
        EmitSignal(SignalName.UnitAttacked, Unit, target);
        GD.Print($"'{Unit.Stats.UnitId}' Attacks '{target.Stats.UnitId}' with '{Unit.Stats.Attack}'dmg");
        
        return true;
    }
}