#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using AKidsDream.Commands;
using AKidsDream.Utilities.Errors;
using AKidsDream.Utilities.Logging;
using AKidsDream.Utilities.Results;
using AKidsDream.Core.Content;
using AKidsDream.Core.Systems.ModLoader;
using AKidsDream.Managers.SaveSystems;
using AKidsDream.Util.Identifiers;
using AKidsDream.Utilities.Registry;
using Godot;
using Serilog;

namespace AKidsDream.Core.Managers.ModManager;

public class ModLoader
{
    private static readonly ILogger Log = GameLogger.For<ModLoader>();

    private readonly GameContext? _ctx;
    private readonly Dictionary<NamespacedKey, IMod> _loadedMods = new();
    private readonly Dictionary<string, ModAssemblyLoadContextCheatSheet> _modLoadContexts = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<NamespacedKey, IMod> LoadedMods => _loadedMods;

    public ModLoader(GameContext? ctx = null)
    {
        _ctx = ctx;
    }

    public Result<ModError> Load()
    {
        Log.Here().Info("Starting mod loading lifecycle...");

        // 1. Initialize core content first
        CoreContentInitializer.RegisterCoreContent(new ContentRegistrar(NamespacedKey.DefaultNamespace));

        // 2. Discover mods in Mods directory
        var discoveryResult = DiscoverMods(Global.ModsPath);
        if (discoveryResult.IsFailure)
        {
            Log.Here().Err("Mod discovery failed: {Error}", discoveryResult.Error?.Message);
            return Result.Fail<ModError>(discoveryResult.Error!);
        }

        var discoveredMods = discoveryResult.Value;
        Log.Here().Info("Discovered {Count} valid mod manifests", discoveredMods.Count);

        // 3. Topological Sort dependency graph
        var sortResult = SortByDependencies(discoveredMods);
        if (sortResult.IsFailure)
        {
            Log.Here().Err("Dependency resolution failed: {Error}", sortResult.Error?.Message);
            return Result.Fail(sortResult.Error!);
        }

        var sortedMods = sortResult.Value;

        // 4. Load mods in dependency order
        foreach (var (manifest, modDir) in sortedMods)
        {
            var loadResult = LoadSingleMod(manifest, modDir);
            if (loadResult.IsFailure)
            {
                Log.Here().Err("Failed to load mod '{ModId}': {Error}", manifest.Id, loadResult.Error?.Message);
                return loadResult;
            }
        }

        // 5. Freeze all content registries
        Global.ModRegistry.Freeze();
        Global.UnitRegistry.Freeze();
        Global.CardRegistry.Freeze();
        Global.GameModeRegistry.Freeze();
        Global.ControllerRegistry.Freeze();
        Global.ConsoleCommandRegistry.Freeze();

        // 6. Initialize mods if GameContext is present
        if (_ctx != null)
        {
            InitMods(_ctx);
        }

        Log.Here().Info("Mod loading pipeline completed successfully. Total mods active: {ModCount}", _loadedMods.Count);
        return Result.Ok<ModError>();
    }

    public void InitMods(GameContext ctx)
    {
        Log.Here().Info("Initializing {Count} loaded mods with GameContext...", _loadedMods.Count);
        foreach (var mod in _loadedMods.Values)
        {
            try
            {
                mod.Init(ctx);
            }
            catch (Exception ex)
            {
                Log.Here().Err(ex, "Exception thrown during mod Init for {ModType}", mod.GetType().FullName);
            }
        }
    }

    public Result<List<(ModManifest Manifest, string Directory)>, ModError> DiscoverMods(string modsRootPath)
    {
        var discovered = new List<(ModManifest Manifest, string Directory)>();

        if (!Directory.Exists(modsRootPath))
        {
            Log.Here().Warn("Mods directory does not exist at '{ModsPath}'", modsRootPath);
            return Result.Ok<List<(ModManifest Manifest, string Directory)>, ModError>(discovered);
        }

        var subDirs = Directory.GetDirectories(modsRootPath);
        foreach (var dir in subDirs)
        {
            var manifestFile = Path.Combine(dir, "manifest.json");
            if (!File.Exists(manifestFile))
                continue;

            try
            {
                var json = File.ReadAllText(manifestFile);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var manifest = JsonSerializer.Deserialize<ModManifest>(json, options);

                if (manifest == null)
                {
                    Log.Here().Warn("Failed to deserialize manifest at '{Path}'", manifestFile);
                    continue;
                }

                var validationResults = ModValidator.ValidateManifest(manifest, dir);
                var failures = validationResults.Where(x => x.IsFailure).ToArray();
                if (failures.Any()) 
                {
                    foreach (var err in failures)
                    {
                        Log.Here().Err("Manifest validation failed for '{Dir}': {Error}", dir, err.Error?.Message);
                    }
                    continue;
                }

                discovered.Add((manifest, dir));
            }
            catch (Exception ex)
            {
                Log.Here().Err(ex, "Error reading manifest at '{Path}'", manifestFile);
                // return Result.Fail<List<(ModManifest, string)>, ModError>(new ModError.LoadFailed(Path.GetFileName(dir), ex.Message));
            }
        }

        return Result.Ok<List<(ModManifest, string)>, ModError>(discovered);
    }

    public Result<List<(ModManifest Manifest, string Directory)>, ModError> SortByDependencies(
        List<(ModManifest Manifest, string Directory)> mods)
    {
        //TODO: implement
        return Result.Ok<List<(ModManifest, string)>, ModError>(mods);
    }

    private Result<ModError> LoadSingleMod(ModManifest manifest, string modDir)
    {
        Log.Here().Info("Loading mod '{ModName}' ({ModId} v{Version})...", manifest.Name, manifest.Id, manifest.Version);

        // 1. Mount Godot .pck resource pack if present
        var pckPath = Path.Combine(modDir, $"{manifest.ResourcePackPath}.pck");
        if (File.Exists(pckPath))
        {
            bool mounted = ProjectSettings.LoadResourcePack(pckPath);
            Log.Here().Info("Mounted PCK pack '{PckPath}': {Success}", pckPath, mounted);
        }

        // 2. Prepare dependency ALCs

        // 3. Create Mod ALC
        var entryPath = Path.Combine(modDir, manifest.EntryPoint);
        ModLoadContext modLoadContext = new(manifest.Id, entryPath);
        Assembly modAlc;
        try
        {
            modAlc = modLoadContext.LoadFromAssemblyPath(entryPath);
        }
        catch (Exception ex)
        {
            Log.Here().Err(ex, "Failed to load mod '{ModId}' entry point assembly '{EntryPath}'", manifest.Id, entryPath);
            return Result.Fail<ModError>(new ModError.LoadFailed(manifest.Id, ex.Message));
        }

        // 4. Instantiate entry point
        IMod? modInstance = null;
        try
        {
            if (manifest.EntryPoint.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                var modTypes = modAlc
                    .GetTypes()
                    .Where(t =>
                        typeof(IMod).IsAssignableFrom(t) &&
                        t is { IsAbstract: false, IsInterface: false }).ToArray();

                if (modTypes.Length > 1)
                    return Result.Fail<ModError>(new ModError.LoadFailed(manifest.Id, "Multiple mod types which implement IMod found"));
                if (modTypes.Length == 0)
                    return Result.Fail<ModError>(new ModError.LoadFailed(manifest.Id, "No mod type which implements IMod found"));

                if (Activator.CreateInstance(modTypes[0]) is not IMod mod)
                    return Result.Fail<ModError>(new ModError.LoadFailed(manifest.Id, "Mod type which implements IMod found"));

                modInstance = mod;
            }
        }
        catch (Exception ex)
        {
            Log.Here().Err(ex, "Failed to instantiate mod '{ModId}' entry point assembly '{EntryPath}'", manifest.Id, entryPath);
            return Result.Fail<ModError>(new ModError.LoadFailed(manifest.Id, ex.Message));
        }
        
        if (modInstance == null)
            return Result.Fail<ModError>(new ModError.LoadFailed(manifest.Id, "Failed to instantiate mod entry point"));
        

        // 5. Register content with scoped registrar
        var registrar = new ContentRegistrar(manifest.Id);
        try
        {
            modInstance.RegisterContent(registrar);
            Global.ModRegistry.Register(manifest.Id, manifest);
            _loadedMods.Add(manifest.Id, modInstance);
            Log.Here().Info("Mod '{ModId}' content registered successfully", manifest.Id);
        } 
        catch (Exception ex)
        {
            Log.Here().Err(ex, "Failed to register content for mod '{ModId}'", manifest.Id);
            return Result.Fail<ModError>(new ModError.LoadFailed(manifest.Id, ex.Message));
        }

        return Result.Ok<ModError>();
    }
}
