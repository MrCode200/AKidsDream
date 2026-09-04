#nullable enable
using System.Diagnostics.CodeAnalysis;
using AKidsDream.Common.Logging;
using AKidsDream.Util.Identifiers;
using AKidsDream.GameBoard;
using AKidsDream.Common;
using Godot;
using Godot.Collections;
using Serilog;

namespace AKidsDream.Managers.SaveSystems.Rehydration;

/// <summary>
/// Sole responsibility: build live <see cref="Unit"/> scene instances from saved
/// <see cref="UnitStateData"/> and add them under the given parent node.
/// Ownership validation is delegated to <see cref="UnitOwnershipResolver"/>; scene
/// loading and instantiation live here.
/// </summary>
public static class UnitStateInitializer
{
    private static readonly ILogger Log = GameLogger.For(typeof(UnitStateInitializer));

    public static Array<Unit> InitializeUnits(
        Node parent,
        Array<UnitStateData>? savedUnits,
        PlayerTeamRegistry playerTeamRegistry,
        Board board
    )
    {
        var initializedUnits = new Array<Unit>();

        if (savedUnits == null)
            return initializedUnits;
        
        foreach (var state in savedUnits)
        {
            if (TryCreateUnit(parent, state, playerTeamRegistry, board, out var unit))
                initializedUnits.Add(unit);
        }

        return initializedUnits;
    }

    private static bool TryCreateUnit(
        Node parent,
        UnitStateData state,
        PlayerTeamRegistry playerTeamRegistry,
        Board board,
        [NotNullWhen(true)] out Unit? unit)
    {
        unit = null;

        Log.Here()
            .Debug("Attempting to create unit '{UnitName}' at {TileLocation}",
                state.UnitName, state.TileLocation);

        if (!UnitOwnershipResolver.TryResolve(state, playerTeamRegistry, out var ownership))
            return false;

        var unitName = state.UnitName.ToString();
        var unitScene = LoadUnitScene(unitName);
        if (unitScene == null)
            return false;

        UnitId? unitId = state.UnitId >= 1 ? new UnitId(state.UnitId) : null;
        var newUnit = unitScene.Instantiate<Unit>();

        newUnit.Init(
            ownership.Value.PlayerData,
            state.TileLocation,
            state.UnitStats,
            board,
            unitId
        );

        // Set position directly (rather than via movement) to skip the signal emission
        // that a normal Unit move would trigger during initial load.
        newUnit.Position = Board.TileToWorldPosition(state.TileLocation);

        parent.AddChild(newUnit);

        unit = newUnit;
        return true;
    }

    private static PackedScene? LoadUnitScene(string unitName)
    {
        var scenePath = $"res://Entities/Units/{unitName}/{unitName}.tscn";
        var unitScene = GD.Load<PackedScene>(scenePath);

        if (unitScene == null)
        {
            Log.Here().Warn("Failed to load unit scene at '{ScenePath}' for '{UnitName}'", scenePath, unitName);
        }

        return unitScene;
    }
}