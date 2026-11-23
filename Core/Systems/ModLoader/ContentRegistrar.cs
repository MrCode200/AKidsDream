using System;
using AKidsDream.Utilities.Logging;
using AKidsDream.Core.Content;
using AKidsDream.Entities.Cards;
using AKidsDream.Managers.SaveSystems;
using AKidsDream.Util.Identifiers;
using AKidsDream.Utilities.Registry;
using Godot;
using Serilog;

namespace AKidsDream.Core.Managers.ModManager;

public interface IContentRegistrar
{
    string ModId { get; }
    IScopedRegistry<UnitDefinition> Units { get; }
    IScopedRegistry<CardDefinition> Cards { get; }
    IScopedRegistry<GameModeDefinition> GameModes { get; }
    IScopedRegistry<ControllerDefinition> Controllers { get; }
    IScopedRegistry<ConsoleCommandDefinition> ConsoleCommands { get; }

    void RegisterUnit(string path, UnitDefinition definition);
    void RegisterCard(string path, CardDefinition definition);
    void RegisterGameMode(string path, GameModeDefinition definition);
    void RegisterController(string path, ControllerDefinition definition);
    void RegisterConsoleCommand(string path, ConsoleCommandDefinition definition);
}

// New thing learned: Registrar / a rar is sth registers stuff 
public class ContentRegistrar : IContentRegistrar
{
    private static readonly ILogger Log = GameLogger.For<ContentRegistrar>();

    public string ModId { get; }
    public IScopedRegistry<UnitDefinition> Units { get; }
    public IScopedRegistry<CardDefinition> Cards { get; }
    public IScopedRegistry<GameModeDefinition> GameModes { get; }
    public IScopedRegistry<ControllerDefinition> Controllers { get; }
    public IScopedRegistry<ConsoleCommandDefinition> ConsoleCommands { get; }

    public ContentRegistrar(string modId)
    {
        ModId = string.IsNullOrWhiteSpace(modId) ? NamespacedKey.DefaultNamespace : modId.Trim().ToLowerInvariant();
        Units = new ScopedRegistry<UnitDefinition>(Global.UnitRegistry, ModId);
        Cards = new ScopedRegistry<CardDefinition>(Global.CardRegistry, ModId);
        GameModes = new ScopedRegistry<GameModeDefinition>(Global.GameModeRegistry, ModId);
        Controllers = new ScopedRegistry<ControllerDefinition>(Global.ControllerRegistry, ModId);
        ConsoleCommands = new ScopedRegistry<ConsoleCommandDefinition>(Global.ConsoleCommandRegistry, ModId);

        Log.Here().Debug("Created ContentRegistrar for mod '{ModId}'", ModId);
    }

    public void RegisterUnit(string path, UnitDefinition definition)
    {
        var key = new NamespacedKey(ModId, path);
        var def = definition with { Id = key };
        Log.Here().Debug("Registering unit '{UnitId}' for mod '{ModId}'", key, ModId);
        Units.Register(path, def);
    }

    public void RegisterCard(string path, CardDefinition definition)
    {
        var key = new NamespacedKey(ModId, path);
        var def = definition with { Id = key };
        Log.Here().Debug("Registering card from AbilityCardData '{CardId}' for mod '{ModId}'", key, ModId);
        Cards.Register(path, def);
    }

    public void RegisterGameMode(string path, GameModeDefinition definition)
    {
        var key = new NamespacedKey(ModId, path);
        var def = definition with { Id = key };
        Log.Here().Debug("Registering game mode '{GameModeId}' for mod '{ModId}'", key, ModId);
        GameModes.Register(path, def);
    }

    public void RegisterController(string path, ControllerDefinition definition)
    {
        var key = new NamespacedKey(ModId, path);
        var def = definition with { Id = key };
        Log.Here().Debug("Registering controller '{ControllerId}' for mod '{ModId}'", key, ModId);
        Controllers.Register(path, def);
    }

    public void RegisterConsoleCommand(string path, ConsoleCommandDefinition definition)
    {
        var key = new NamespacedKey(ModId, path);
        var def = definition with { Id = key };
        Log.Here().Debug("Registering console command '{ConsoleCommandId}' for mod '{ModId}'", key, ModId);
        ConsoleCommands.Register(path, def);
    }
}
