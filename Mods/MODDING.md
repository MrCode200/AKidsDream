# Modding Guide for A Kid's Dream

## 1. Overview & Architecture

*A Kid's Dream* uses a conflict-free, namespaced registry architecture and isolated `.NET` `AssemblyLoadContext` runtime for mods.

### Key Highlights
- **Namespaced Identifiers (`NamespacedKey`)**: All content entries (units, cards, gamemodes, controllers, console commands) are identified as `mod_id:content_path` (e.g. `core:soldier`, `custom_mod:archer`).
- **Scoped Registration (`IContentRegistrar`)**: Mods register content using a scoped registrar that automatically prefixes registered items with their mod ID.
- **Type-Safe Definitions**: Content definitions bundle metadata, packed scene prefabs, and factory delegates.
- **Developer Console Commands**: Mods can register custom developer console commands with argument metadata and tab autocompletion.
- **External C# Dependency Isolation**: Mods can package third-party NuGet DLLs in `Mods/<ModId>/libs/` without causing assembly version conflicts with the engine or other mods.

---

## 2. Creating a Mod

Create a directory under `Mods/<YourModId>/` containing a `manifest.json`.

### Example `manifest.json`
```json
{
  "id": "chaos_mod",
  "name": "Chaos Overhaul",
  "author": "ModAuthor",
  "version": "1.0.0",
  "apiVersion": 1,
  "dependencies": ["core"],
  "entryPoint": "ChaosModEntry.cs",
  "description": "Adds new units, cards, and custom console commands."
}
```

### Implementing `IMod`
Create an entry point class implementing `IMod`:

```csharp
using AKidsDream.Commands;
using AKidsDream.Core.Content;
using AKidsDream.Core.Managers.ModManager;
using AKidsDream.Entities.Cards;
using AKidsDream.Util.Identifiers;
using Godot;

namespace ChaosMod;

public class ChaosModEntry : IMod
{
    public void RegisterContent(IContentRegistrar registrar)
    {
        // 1. Register a custom Unit (automatically namespaced as chaos_mod:demon)
        registrar.RegisterUnit("demon", new UnitDefinition(
            Id: new NamespacedKey(registrar.ModId, "demon"),
            DisplayName: "Chaos Demon",
            Description: "High-damage demonic attacker",
            Icon: null,
            Prefab: ResourceLoader.Load<PackedScene>("res://Mods/chaos_mod/Units/Demon/Demon.tscn")
        ));

        // 2. Register custom Console Command (invoked via `chaos_mod:spawn_demon` or `spawn_demon`)
        registrar.RegisterConsoleCommand("spawn_demon", new ConsoleCommandDefinition(
            Id: new NamespacedKey(registrar.ModId, "spawn_demon"),
            Description: "Spawns a chaos demon at specified coordinates",
            ArgumentNames: new[] { "playerId", "tileX", "tileY" },
            RequiredArguments: 3,
            Handler: execContext =>
            {
                execContext.PrintLine("Spawned demon for player " + execContext.Args[0]);
            }
        ));
    }

    public void Init(GameContext ctx)
    {
        // Called when game starts with the active GameContext
    }
}
```

---

## 3. External Dependencies & NuGet Libraries

If your mod relies on external C# DLLs:
1. Place third-party DLLs in `Mods/<YourModId>/libs/` (e.g. `Mods/chaos_mod/libs/Newtonsoft.Json.dll`).
2. The mod's `ModAssemblyLoadContext` will automatically resolve private dependencies from `libs/` without polluting the global process space.
3. Engine contracts (`AKidsDream.*`, `GodotSharp`, `System.*`) are automatically shared across all mods.
