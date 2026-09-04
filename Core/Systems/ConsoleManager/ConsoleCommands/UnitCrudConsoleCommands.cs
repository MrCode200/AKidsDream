#nullable enable
using System;
using System.Linq;
using AKidsDream.Common.Logging;
using Godot;
using Godot.Collections;
using AKidsDream.Common;
using AKidsDream.Managers.SaveSystems;
using AKidsDream.Managers.SaveSystems.Rehydration;
using Serilog;

namespace AKidsDream.Util.Identifiers.Commands;

[ConsoleCommand]
public partial class UnitCrudConsoleCommands : ConsoleCommandBase
{
    private static readonly ILogger Log = GameLogger.For(typeof(UnitCrudConsoleCommands));
    private static readonly string[] ValidUnitNames = Enum.GetNames(typeof(Global.UnitName));

    public override void _Ready()
    {
        Console.AddCommand("create_unit", new Callable(this, nameof(CreateUnit)),
            ["unitName", "playerId", "teamId", "tileX", "tileY"],
            5, "Create a unit at specified tile location");
        Console.AddCommandAutocompleteList("create_unit", ValidUnitNames);
        Log.Here().Debug("Registered command 'create_unit'");
    }

    public override void _ExitTree()
    {
        Console.RemoveCommand("create_unit");
        Log.Here().Debug("Unregistered command 'create_unit'");
    }

    // -- Console Commands --

    private void CreateUnit(string unitName, string playerId, string tileX, string tileY)
    {
        if (!RequireContext()) return;

        var ok = true;
        ok &= TryInt(playerId, "playerId", out var playerIdInt,
            allowedValues: [.. Context.PlayerTeamRegistry.GetAllPlayers().Select(p => p.PlayerId.Value)]);
        ok &= TryInt(tileX, "tileX", out var tileXInt, min: 0, max: Context.Board.StateData.Width);
        ok &= TryInt(tileY, "tileY", out var tileYInt, min: 0, max: Context.Board.StateData.Height);
        ok &= TryEnum<Global.UnitName>(unitName, "unitName", out var parsedUnitName);

        if (!ok) return;

        var unitStateData = new UnitStateData
        {
            UnitId = 0,
            OwnerId = playerIdInt,
            UnitName = parsedUnitName,
            TileLocation = new Vector2I(tileXInt, tileYInt),
            UnitStats = GD.Load<UnitStatsData>($"res://Entities//Units//{unitName}//{unitName}Stats.tres")
        };

        var unitsArray = new Array<UnitStateData> { unitStateData };
        var createdUnits =
            UnitStateInitializer.InitializeUnits(
                Context.EntityLayer,
                unitsArray,
                Context.PlayerTeamRegistry,
                Context.Board
            );

        if (createdUnits.Count > 0)
        {
            Log.Here().Info("Creating unit {UnitName}:{UnitId} at ({TileX}, {TileY}) for player {PlayerId}",
                unitName, createdUnits[0].UnitId, tileXInt, tileYInt, playerIdInt);
            Console.PrintLine(
                $"Created unit {parsedUnitName} at tile ({tileXInt}, {tileYInt}) for player {playerIdInt}");
        }
        else
        {
            Console.PrintError("Failed to create unit");
        }
    }
}