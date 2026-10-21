#nullable enable
using System;
using System.Linq;
using AKidsDream.Commands;
using AKidsDream.Utilities.Logging;
using Godot;
using Godot.Collections;
using AKidsDream.Utilities;
using AKidsDream.Managers.SaveSystems;
using AKidsDream.Managers.SaveSystems.Rehydration;
using Serilog;

namespace AKidsDream.Util.Identifiers.Commands;

public partial class UnitCrudConsoleCommands : ConsoleCommandBase
{
    private static readonly ILogger Log = GameLogger.For(typeof(UnitCrudConsoleCommands));
    private static readonly string[] ValidUnitNames = Global.UnitRegistry.GetAllKeys().Select(u => u.ToString()).ToArray();
    
    private GameContext _context = null!;

    public override void Init(GameContext context)
    {
        _context = context;
        
        Console.AddCommand("create_unit", new Callable(this, nameof(CreateUnit)),
            ["unitName", "playerId", "teamId", "tileX", "tileY"],
            5, "Create a unit at specified tile location");
        Console.AddCommandAutocompleteList("create_unit", ValidUnitNames);
        Log.Here().Debug("Registered command 'create_unit'");
    }

    public override void Exit()
    {
        Console.RemoveCommand("create_unit");
        Log.Here().Debug("Unregistered command 'create_unit'");
    }

    // -- Console Commands --

    private void CreateUnit(string unitKey, string playerId, string tileX, string tileY)
    {
        var ok = true;
        ok &= TryInt(playerId, "playerId", out var playerIdInt,
            allowedValues: [.. _context.PlayerTeamRegistry.GetAllPlayers().Select(p => p.PlayerId.Value)]);
        ok &= TryInt(tileX, "tileX", out var tileXInt, min: 0, max: _context.Board.State.Width);
        ok &= TryInt(tileY, "tileY", out var tileYInt, min: 0, max: _context.Board.State.Height);
        ok &= StringIn(unitKey, ValidUnitNames);

        if (!ok) return;

        var unitStateData = new UnitStateData
        {
            UnitId = 0,
            OwnerId = playerIdInt,
            UnitKey = unitKey,
            TileLocation = new Vector2I(tileXInt, tileYInt),
            UnitStats = GD.Load<UnitStatsData>($"res://Entities//Units//{unitKey}//{unitKey}Stats.tres")
        };

        var unitsArray = new Array<UnitStateData> { unitStateData };
        var createdUnits =
            UnitStateInitializer.InitializeUnits(
                _context.EntityLayer,
                unitsArray,
                _context.PlayerTeamRegistry,
                _context.Board
            );

        if (createdUnits.Count > 0)
        {
            Log.Here().Info("Creating unit {UnitName}:{UnitId} at ({TileX}, {TileY}) for player {PlayerId}",
                unitKey, createdUnits[0].UnitId, tileXInt, tileYInt, playerIdInt);
            Console.PrintLine(
                $"Created unit {unitKey} at tile ({tileXInt}, {tileYInt}) for player {playerIdInt}");
        }
        else
        {
            Log.Here().Err("Failed to create unit. CreatedUnits.Count == 0, unitStateData: {UnitStateData}", unitStateData);
            Console.PrintError("Failed to create unit");
        }
    }
}