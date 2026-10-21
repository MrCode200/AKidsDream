#nullable enable
using AKidsDream.Utilities;
using AKidsDream.Utilities.Logging;
using AKidsDream.Controllers;
using AKidsDream.Core.Managers;
using AKidsDream.Core.Managers.ModManager;
using AKidsDream.Entities.Cards;
using AKidsDream.Util.Identifiers;
using AKidsDream.Util.Identifiers.Commands;
using Godot;
using Serilog;

namespace AKidsDream.Core.Content;

public static class CoreContentInitializer
{
    private static readonly ILogger Log = GameLogger.For(typeof(CoreContentInitializer));
    
    public static void RegisterCoreContent(IContentRegistrar? registrar = null)
    {
        registrar ??= new ContentRegistrar(NamespacedKey.DefaultNamespace);

        // Register core units
        registrar.RegisterUnit("soldier", new UnitDefinition(
            Id: new NamespacedKey(registrar.ModId, "soldier"),
            DisplayName: "Soldier",
            Description: "Standard melee warrior",
            Icon: null,
            Prefab: ResourceLoader.Load<PackedScene>("res://Core/Content/Units/Soldier/Soldier.tscn"),
            DefaultStats: ResourceLoader.Load<UnitStatsData>("res://Core/Content/Units/Soldier/SoldierStats.tres")
        ));

        // Register core cards
        var soldierCardData = ResourceLoader.Load<AbilityCardData>("res://Core/Content/Units/Soldier/SoldierCardData.tres");
        if (soldierCardData != null)
        {
            registrar.RegisterCard("spawn_soldier", new CardDefinition(
                Id: new NamespacedKey(registrar.ModId, "spawn_soldier"),
                CardData: soldierCardData
            ));
        }

        // Register core game modes
        registrar.RegisterGameMode("kingslayer", new GameModeDefinition(
            Id: new NamespacedKey(registrar.ModId, "kingslayer"),
            DisplayName: "King Slayer",
            Description: "Defeat the enemy king to win",
            Factory: ctx => new KingSlayerGameMode()
        ));

        // Register core controllers
        var controllerPath = nameof(ControllerType.PlayerInteractionController).ToSnakeCase();
        registrar.RegisterController(controllerPath, new ControllerDefinition(
            Id: new NamespacedKey(registrar.ModId, controllerPath),
            DisplayName: "Player Interaction Controller",
            Description: "Standard player interaction controller",
            Factory: (ctx, playerData) => new InteractionController(ctx, playerData)
        ));

        // Register core console commands
        registrar.RegisterConsoleCommand("crud_unit_command", new ConsoleCommandDefinition(
            Id: new NamespacedKey(registrar.ModId, "crud_unit_command"),
            Command: new UnitCrudConsoleCommands()
        ));
        
        Log.Here().Info("Core content registered");
    }
}
