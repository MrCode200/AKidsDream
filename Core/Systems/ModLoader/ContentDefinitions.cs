#nullable enable
using System;
using AKidsDream.Commands;
using AKidsDream.Utilities;
using AKidsDream.Core.Managers.GameModeManager;
using AKidsDream.Core.Teams;
using AKidsDream.Entities.Cards;
using AKidsDream.Util.Identifiers;
using AKidsDream.Util.Identifiers.Commands;
using Godot;

namespace AKidsDream.Core.Content;

public interface IObjectDefinition
{
    public NamespacedKey Id { get; }
}

public record UnitDefinition(
    string DisplayName,
    string Description,
    Texture2D? Icon,
    PackedScene? Prefab,
    UnitStatsData? DefaultStats = null,
    Func<Unit>? Factory = null
) : IObjectDefinition
{
    public NamespacedKey Id { get; internal init; }
    
    public Unit Instantiate()
    {
        if (Factory != null) return Factory();
        if (Prefab != null) return Prefab.Instantiate<Unit>();
        throw new InvalidOperationException($"UnitDefinition '{Id}' has neither a Prefab nor a Factory.");
    }
}

public record CardDefinition(
    AbilityCardData CardData
) : IObjectDefinition
{
    public NamespacedKey Id { get; internal init; }
}

public record GameModeDefinition(
    string DisplayName,
    string Description,
    Func<GameContext, IGameMode> Factory
) : IObjectDefinition
{
    public NamespacedKey Id { get; internal init; }
}

public record ControllerDefinition(
    string DisplayName,
    string Description,
    Func<GameContext, PlayerData, IController> Factory
) : IObjectDefinition
{
    public NamespacedKey Id { get; internal init; }
}

public record ConsoleCommandDefinition(
    IConsoleCommand Command
) : IObjectDefinition
{
    public NamespacedKey Id { get; internal init; }
}
