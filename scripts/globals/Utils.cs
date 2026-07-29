using System;
using System.Reflection;
using AKidsDream.GameBoard;
using Godot;
using TileData = AKidsDream.GameBoard.TileData;

namespace AKidsDream.Globals;

// NOTE:
// This is a singleton class, but contains only statics
// Maybe Remove singleton...
/// <summary>
/// Global utility class providing helper functions for resource management and other common operations.
/// This class implements the singleton pattern and should be added to the autoloaded singletons in Godot.
/// </summary>
[Tool]
public partial class Utils : Node2D
{
    /// <summary>
    /// Gets the singleton instance of the Utils class.
    /// </summary>
    public static Utils Instance { get; private set; }

    private static int _nextId;

    /// <summary>
    /// Called when the node is added to the scene tree. Initializes the singleton instance.
    /// </summary>
    public override void _Ready()
    {
        Instance = this;
    }

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
    public static T RebuildTyped<T>(Resource source) where T : Resource, new()
    {
        if (source == null) return null;

        T target = new T();
        foreach (var prop in source.GetPropertyList())
        {
            string name = prop["name"].AsString();
            var usage = prop["usage"].As<PropertyUsageFlags>();

            if ((usage & PropertyUsageFlags.Storage) == 0) continue;
            if (name is "resource_path" or "resource_name" or "script") continue;

            target.Set(name, source.Get(name));
        }

        return target;
    }

    public static void ToggleNodeProcessing(Node node, bool enable)
    {
        if (enable)
        {
            node.SetProcessMode(ProcessModeEnum.Inherit);
        }
        else
        {
            node.SetProcessMode(ProcessModeEnum.Disabled);
        }
    }
}

// -- ENUM EXTENSIONS --
/// <summary>
/// Attribute used to associate a typed value with enum fields.
/// This allows enum values to have additional metadata that can be retrieved via extension methods.
/// </summary>
/// <example>
/// <code>
/// public enum MyEnum
/// {
///     [FieldValue<string>("First Option")]
///     First,
///     
///     [FieldValue<int>(42)]
///     Second
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Field)]
public class FieldValue<T> : Attribute
{
    /// <summary>
    /// Gets the T value associated with this attribute.
    /// </summary>
    public T Value { get; }

    /// <summary>
    /// Initializes a new instance of the FieldValue attribute.
    /// </summary>
    /// <param name="value">The T value to associate with the enum field.</param>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    public FieldValue(T value)
    {
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }
}

/// <summary>
/// Provides extension methods for enum types, specifically for retrieving FieldValue attributes.
/// </summary>
public static class EnumExtensions
{
    /// <summary>
    /// Retrieves the FieldValue associated with an enum field.
    /// </summary>
    /// <typeparam name="T">The type of value to retrieve.</typeparam>
    /// <param name="value">The enum value to get the typed value for.</param>
    /// <param name="ignoreMissingValue">
    /// If true, returns default(T) when no FieldValue attribute is found.
    /// If false, throws an ArgumentException when no attribute is found.
    /// </param>
    /// <returns>
    /// The typed value from the FieldValue attribute, or default(T) if ignoreMissingValue is true and no attribute is found.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when ignoreMissingValue is false and no FieldValue attribute is found on the enum field.
    /// </exception>
    /// <example>
    /// <code>
    /// MyEnum value = MyEnum.First;
    /// string stringValue = value.GetFieldValue<string>(); // Returns "First Option"
    /// int intValue = MyEnum.Second.GetFieldValue<int>(); // Returns 42
    /// </code>
    /// </example>
    public static T GetFieldValue<T>(this Enum value, bool ignoreMissingValue = false)
    {
        var field = value.GetType().GetField(value.ToString());
        var attr = field?.GetCustomAttribute<FieldValue<T>>();

        if (attr == null)
        {
            if (ignoreMissingValue) return default;
            throw new ArgumentException($"No FieldValue<{typeof(T).Name}> attribute found for {value}");
        }

        return attr.Value;
    }
}