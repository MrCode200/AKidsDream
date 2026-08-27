using System;
using AKidsDream.Common.Logging;
using Godot;
using Godot.Collections;
using AKidsDream.Common;
using AKidsDream.Managers.SaveSystems;
using AKidsDream.Managers.SaveSystems.Rehydration;
using Serilog;

namespace AKidsDream.Core.Controllers.Commands;

[ConsoleCommand]
public partial class UnitCrudConsoleCommands : Node
{
    private static readonly ILogger Log = GameLogger.For(typeof(UnitCrudConsoleCommands));
    private static readonly string[] ValidUnitNames = Enum.GetNames(typeof(Global.UnitName));
    private static Node EntityLayer = null!;
    
    public override void _Ready()
    {
        EntityLayer = GetNode<Node>("/root/GameWorld/EntityLayer");
        
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
    
    private void CreateUnit(string unitName, string playerId, string teamId, string tileX, string tileY)
    {
        if (!int.TryParse(playerId, out var playerIdInt))
        {
            Console.PrintError("Invalid playerId: must be an integer");
            return;
        }

        if (!int.TryParse(teamId, out var teamIdInt))
        {
            Console.PrintError("Invalid teamId: must be an integer");
            return;
        }

        if (!int.TryParse(tileX, out var tileXInt))
        {
            Console.PrintError("Invalid tileX: must be an integer");
            return;
        }

        if (!int.TryParse(tileY, out var tileYInt))
        {
            Console.PrintError("Invalid tileY: must be an integer");
            return;
        }

        if (!System.Enum.TryParse<Global.UnitName>(unitName, true, out var parsedUnitName))
        {
            Console.PrintError($"Invalid unitName: {unitName}.");
            return;
        }

        var unitStateData = new UnitStateData
        {
            UnitId = 0,
            OwnerId = playerIdInt,
            UnitName = parsedUnitName,
            TileLocation = new Vector2I(tileXInt, tileYInt),
            UnitStats = GD.Load<UnitStatsData>($"res://Entities//Units//{unitName}//{unitName}Stats.tres")
        };

        var unitsArray = new Array<UnitStateData> { unitStateData };
        var createdUnits = UnitStateInitializer.InitializeUnits(EntityLayer, unitsArray);

        if (createdUnits.Count > 0)
        {
            Log.Here().Info("Creating unit {UnitName}:{UnitId} at ({TileX}, {TileY}) for player {PlayerId}", 
                unitName, createdUnits[0].UnitId, tileXInt, tileYInt, playerIdInt);
            Console.PrintLine($"Created unit {parsedUnitName} at tile ({tileXInt}, {tileYInt}) for player {playerIdInt}");
        }
        else
        {
            Console.PrintError("Failed to create unit");
        }
    }

    private void TestCommand()
    {
        Console.PrintLine("Test command works!");
        Log.Here().Info("Test command executed");
    }
}