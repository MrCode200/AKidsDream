using System;
using System.Reflection;
using Godot;
using Array = Godot.Collections.Array;

namespace AKidsDream.Utilities;

/// <summary>
/// Global utility class providing helper functions for resource management and other common operations.
/// </summary>
[Tool]
public  class Utils
{
    private static int _nextId;

    public static void SetNextId(int id)
    {
        if (id < _nextId)
            GD.PushWarning("NextId is set to less than to the current id. " +
                           "This can cause issues with id generation.");
        _nextId = id;
    }

    /// <summary>
    /// A super simple incremental id generator, local to this class.
    /// Doesn't do any id checks.
    /// </summary>
    /// <returns>The next available id</returns>
    public static int GetNextId()
    {
        return _nextId++;
    }

    /// <summary>
    /// Rebuilds a resource by copying all properties from a source resource to a new typed instance.
    /// </summary>
    /// <typeparam name="T">The target resource type, must inherit from Resource and have a parameterless constructor.</typeparam>
    /// <param name="source">The source resource to copy properties from.</param>
    /// <returns>A new instance of type T with all properties copied from the source, or null if source is null.</returns>
    /// <remarks>
    /// This method copies all storage properties except for resource_path, resource_name, and script.
    /// It uses Godot's property list to dynamically copy properties.
    /// </remarks>
    public static T RebuildResourceAsT<T>(Resource source) where T : Resource, new()
    {
        if (source == null) return null;

        T target = new T();
        foreach (var prop in source.GetPropertyList())
        {
            string name = prop["name"].AsString();
            var usage = prop["usage"].As<PropertyUsageFlags>();

            if ((usage & PropertyUsageFlags.Storage) == 0) continue;
            if (name is "resource_path" or "resource_name" or "script") continue;

            var value = source.Get(name);

            target.Set(name, value);
        }

        return target;
    }
}

// -- ENUM EXTENSIONS --
