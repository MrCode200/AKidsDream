#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using AKidsDream.Utilities.Logging;
using Serilog;

namespace AKidsDream.Core.Managers.ModManager;

public class ModAssemblyLoadContextCheatSheet : AssemblyLoadContext
{
    private static readonly ILogger Log = GameLogger.For<ModAssemblyLoadContextCheatSheet>();

    public string ModId { get; }
    public string ModDirectory { get; }

    private readonly List<ModAssemblyLoadContextCheatSheet> _dependencyContexts;

    public ModAssemblyLoadContextCheatSheet(string modId, string modDirectory, IEnumerable<ModAssemblyLoadContextCheatSheet>? dependencyContexts = null)
        : base(name: $"Mod_{modId}", isCollectible: true)
    {
        ModId = modId;
        ModDirectory = modDirectory;
        _dependencyContexts = dependencyContexts?.ToList() ?? new List<ModAssemblyLoadContextCheatSheet>();
    }

    public void AddDependencyContext(ModAssemblyLoadContextCheatSheet dependencyContextCheatSheet)
    {
        if (!_dependencyContexts.Contains(dependencyContextCheatSheet))
            _dependencyContexts.Add(dependencyContextCheatSheet);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // 1. Shared Host Assemblies (Godot, Core, Engine Contracts) -> delegate to Default ALC
        if (IsHostAssembly(assemblyName.Name))
            return null;

        // 2. Mod-local libraries (Mods/<ModId>/libs/*.dll or root *.dll)
        if (!string.IsNullOrEmpty(ModDirectory) && Directory.Exists(ModDirectory))
        {
            string libsDll = Path.Combine(ModDirectory, "libs", $"{assemblyName.Name}.dll");
            if (File.Exists(libsDll))
            {
                Log.Here().Debug("Loading local library {DllName} for mod {ModId}", assemblyName.Name, ModId);
                return LoadFromAssemblyPath(libsDll);
            }

            string rootDll = Path.Combine(ModDirectory, $"{assemblyName.Name}.dll");
            if (File.Exists(rootDll))
            {
                Log.Here().Debug("Loading local library {DllName} from root for mod {ModId}", assemblyName.Name, ModId);
                return LoadFromAssemblyPath(rootDll);
            }
        }

        // 3. Dependent Mod ALCs
        foreach (var depContext in _dependencyContexts)
        {
            try
            {
                var asm = depContext.LoadFromAssemblyName(assemblyName);
                if (asm != null)
                {
                    Log.Here().Debug("Resolved dependency assembly {DllName} from dependent mod {DepModId}", assemblyName.Name, depContext.ModId);
                    return asm;
                }
            }
            catch
            {
                // Continue searching other dependency contexts
            }
        }

        return null;
    }

    public Assembly LoadModAssembly(string assemblyPath)
    {
        if (!File.Exists(assemblyPath))
            throw new FileNotFoundException($"Mod assembly not found at {assemblyPath}");

        Log.Here().Info("Loading mod assembly '{AssemblyPath}' into {ALCName}", assemblyPath, Name);
        return LoadFromAssemblyPath(assemblyPath);
    }

    public static bool IsHostAssembly(string? name)
    {
        if (string.IsNullOrEmpty(name)) return true;
        return name.StartsWith("AKidsDream", StringComparison.OrdinalIgnoreCase) ||
               name.StartsWith("Godot", StringComparison.OrdinalIgnoreCase) ||
               name.StartsWith("System.", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("System", StringComparison.OrdinalIgnoreCase) ||
               name.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase) ||
               name.StartsWith("netstandard", StringComparison.OrdinalIgnoreCase) ||
               name.StartsWith("mscorlib", StringComparison.OrdinalIgnoreCase) ||
               name.StartsWith("Serilog", StringComparison.OrdinalIgnoreCase);
    }
}
