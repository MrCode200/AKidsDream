#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using AKidsDream.Commands;
using AKidsDream.Utilities.Errors;
using AKidsDream.Utilities.Logging;
using AKidsDream.Utilities.Results;
using AKidsDream.Core.Content;
using AKidsDream.Managers.SaveSystems;
using AKidsDream.Util.Identifiers;
using Godot;
using Serilog;

namespace AKidsDream.Core.Managers.ModManager;

public class ModLoaderCheatSheet
{
    private static readonly ILogger Log = GameLogger.For<ModLoader>();

    private readonly GameContext? _ctx;
    private readonly List<IMod> _loadedMods = new();
    private readonly Dictionary<string, ModAssemblyLoadContextCheatSheet> _modLoadContexts = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<IMod> LoadedMods => _loadedMods;

    public ModLoaderCheatSheet(GameContext? ctx = null)
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

        // 5. Cross-validate(rmvd) content and freeze registries
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
        foreach (var mod in _loadedMods)
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
                return Result.Fail<List<(ModManifest, string)>, ModError>(new ModError.LoadFailed(Path.GetFileName(dir), ex.Message));
            }
        }

        return Result.Ok<List<(ModManifest, string)>, ModError>(discovered);
    }

    public Result<List<(ModManifest Manifest, string Directory)>, ModError> SortByDependencies(
        List<(ModManifest Manifest, string Directory)> mods)
    {
        var modMap = mods.ToDictionary(m => m.Manifest.Id, m => m, StringComparer.OrdinalIgnoreCase);
        var inDegree = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var adjList = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var (manifest, _) in mods)
        {
            inDegree[manifest.Id] = 0;
            adjList[manifest.Id] = new List<string>();
        }

        foreach (var (manifest, _) in mods)
        {
            if (manifest.Dependencies == null) continue;

            foreach (var dep in manifest.Dependencies)
            {
                if (string.Equals(dep, "core", StringComparison.OrdinalIgnoreCase))
                    continue; // Core is loaded by default

                if (!modMap.ContainsKey(dep))
                {
                    return Result.Fail<List<(ModManifest, string)>, ModError>(
                        new ModError.MissingDependency(manifest.Id, dep));
                }

                adjList[dep].Add(manifest.Id);
                inDegree[manifest.Id]++;
            }
        }

        var queue = new Queue<string>(inDegree.Where(kvp => kvp.Value == 0).Select(kvp => kvp.Key));
        var sortedList = new List<(ModManifest Manifest, string Directory)>();

        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();
            sortedList.Add(modMap[currentId]);

            foreach (var dependent in adjList[currentId])
            {
                inDegree[dependent]--;
                if (inDegree[dependent] == 0)
                {
                    queue.Enqueue(dependent);
                }
            }
        }

        if (sortedList.Count != mods.Count)
        {
            var cycleMods = inDegree.Where(kvp => kvp.Value > 0).Select(kvp => kvp.Key);
            var cycleStr = string.Join(" -> ", cycleMods);
            return Result.Fail<List<(ModManifest, string)>, ModError>(
                new ModError.CircularDependency(cycleMods.FirstOrDefault() ?? "unknown", cycleStr));
        }

        return Result.Ok<List<(ModManifest, string)>, ModError>(sortedList);
    }

    private Result<ModError> LoadSingleMod(ModManifest manifest, string modDir)
    {
        Log.Here().Info("Loading mod '{ModName}' ({ModId} v{Version})...", manifest.Name, manifest.Id, manifest.Version);

        // 1. Mount Godot .pck resource pack if present
        var pckPath = Path.Combine(modDir, $"{manifest.Id}.pck");
        if (File.Exists(pckPath))
        {
            bool mounted = ProjectSettings.LoadResourcePack(pckPath);
            Log.Here().Info("Mounted PCK pack '{PckPath}': {Success}", pckPath, mounted);
        }

        // 2. Prepare dependency ALCs
        var depContexts = new List<ModAssemblyLoadContextCheatSheet>();
        if (manifest.Dependencies != null)
        {
            foreach (var dep in manifest.Dependencies)
            {
                if (_modLoadContexts.TryGetValue(dep, out var depCtx))
                {
                    depContexts.Add(depCtx);
                }
            }
        }

        // 3. Create Mod ALC
        var alc = new ModAssemblyLoadContextCheatSheet(manifest.Id, modDir, depContexts);
        _modLoadContexts[manifest.Id] = alc;

        // 4. Instantiate entry point
        IMod? modInstance = null;
        var entryFile = Path.Combine(modDir, manifest.EntryPoint);

        if (manifest.EntryPoint.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var asm = alc.LoadModAssembly(entryFile);
                var modType = asm.GetTypes().FirstOrDefault(t => typeof(IMod).IsAssignableFrom(t) && !t.IsAbstract);
                if (modType != null)
                {
                    modInstance = Activator.CreateInstance(modType) as IMod;
                }
            }
            catch (Exception ex)
            {
                return Result.Fail<ModError>(new ModError.LoadFailed(manifest.Id, $"Failed loading assembly '{entryFile}': {ex.Message}"));
            }
        }
        else
        {
            // C# source file entry point in workspace / host assembly
            var entryTypeName = Path.GetFileNameWithoutExtension(manifest.EntryPoint);
            var hostAsm = Assembly.GetExecutingAssembly();
            var modType = hostAsm.GetTypes().FirstOrDefault(t =>
                typeof(IMod).IsAssignableFrom(t) &&
                !t.IsAbstract &&
                (string.Equals(t.Name, entryTypeName, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(t.FullName, $"{manifest.Id}.{entryTypeName}", StringComparison.OrdinalIgnoreCase)));

            if (modType != null)
            {
                modInstance = Activator.CreateInstance(modType) as IMod;
            }
        }

        if (modInstance == null)
        {
            return Result.Fail<ModError>(new ModError.LoadFailed(manifest.Id, $"Could not find or instantiate IMod entry point '{manifest.EntryPoint}'"));
        }

        // 5. Register content with scoped registrar
        var registrar = new ContentRegistrar(manifest.Id);
        try
        {
            modInstance.RegisterContent(registrar);
            _loadedMods.Add(modInstance);
            Global.ModRegistry.Register(manifest.Id, manifest);
            Log.Here().Info("Mod '{ModId}' content registered successfully", manifest.Id);
        }
        catch (Exception ex)
        {
            return Result.Fail<ModError>(new ModError.LoadFailed(manifest.Id, $"RegisterContent failed: {ex.Message}"));
        }

        return Result.Ok<ModError>();
    }
}
