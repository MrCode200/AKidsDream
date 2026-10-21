#nullable enable
using System;
using System.Reflection;
using System.Runtime.Loader;

namespace AKidsDream.Core.Systems.ModLoader;

// READ TODOS
// TODO: read markdown guide from 19
// TODO: implement cs loading support... (fix validator to also pass .cs files, add logic support to ModLoader)
// TODO:
// contract assembly... (is that just a collection of interfaces (-> thus expose only methods, properties)
// does this mean that i need to craete interfaces for everything moddable?(unit, card and co), 
// but when modding in godot, is it even possible to mod in editor using dll?, if not then user modding is using cs files of main project
// does then still need contract assembly? (as ide and godot will throw if duplicate types and other or not?)
public sealed class ModLoadContext(string modId, string modEntryPath)
    : AssemblyLoadContext(name: $"Mod_{modId}", isCollectible: true)
{
    public string ModId { get; } = modId;

    private readonly AssemblyDependencyResolver _resolver = new(modEntryPath); // TODO: how does resolver actually work?

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        
        if (assemblyPath != null)
            return LoadFromAssemblyPath(assemblyPath);
        
        return null;
    }
    
    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName) // TODO: what is this func, and return type?
    {
        string? libraryPath =
            _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);

        if (libraryPath is null)
            return IntPtr.Zero;

        return LoadUnmanagedDllFromPath(libraryPath);
    }
}