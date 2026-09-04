#nullable enable
using System.Collections.Generic;
using AKidsDream.Managers.SaveSystems;
using Godot;

namespace AKidsDream.Utilities;

/// <summary>
/// Global utility class providing helper functions for resource management and other common operations.
/// </summary>
[Tool]
public static class Utils
{
    public static string GetUnitPath(Global.UnitName unitName) => $"res://Entities/Units/{unitName.ToString()}/";
    
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

    public static IEnumerable<(T1? first, T2? second)> ZipLongest<T1, T2>(
        IEnumerable<T1> first,
        IEnumerable<T2> second
    )
        where T1 : class
        where T2 : class
    {
        using var e1 = first.GetEnumerator();
        using var e2 = second.GetEnumerator();

        while (true)
        {
            bool has1 = e1.MoveNext();
            bool has2 = e2.MoveNext();

            if (!has1 && !has2)
                yield break;

            yield return (
                has1 ? e1.Current : null,
                has2 ? e2.Current : null
            );
        }
    }
}

// -- ENUM EXTENSIONS --