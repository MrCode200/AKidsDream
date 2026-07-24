using System;
using System.Reflection;
using Godot;

namespace AKidsDream.Globals;

/// <summary>
/// Global utility class providing helper functions for resource management and other common operations.
/// This class implements the singleton pattern and should be added to the autoloaded singletons in Godot.
/// </summary>
[Tool]
public partial class Utils : Node
{
	/// <summary>
	/// Gets the singleton instance of the Utils class.
	/// </summary>
	public static Utils Instance { get; private set; }

	/// <summary>
	/// Called when the node is added to the scene tree. Initializes the singleton instance.
	/// </summary>
	public override void _Ready()
	{
		Instance = this;
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
}

/// <summary>
/// Attribute used to associate a string value with enum fields.
/// This allows enum values to have additional string metadata that can be retrieved via extension methods.
/// </summary>
/// <example>
/// <code>
/// public enum MyEnum
/// {
///     [FieldStringValue("First Option")]
///     First,
///     
///     [FieldStringValue("Second Option")]
///     Second
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Field)]
public class FieldStringValue : Attribute
{
	/// <summary>
	/// Gets the string value associated with this attribute.
	/// </summary>
	public string Value { get; }
    
	/// <summary>
	/// Initializes a new instance of the FieldStringValue attribute.
	/// </summary>
	/// <param name="value">The string value to associate with the enum field.</param>
	/// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
	public FieldStringValue(string value)
	{
		Value = value ?? throw new ArgumentNullException(nameof(value));
	}
}

/// <summary>
/// Provides extension methods for enum types, specifically for retrieving FieldStringValue attributes.
/// </summary>
public static class EnumExtensions
{
	/// <summary>
	/// Retrieves the FieldStringValue associated with an enum field.
	/// </summary>
	/// <param name="value">The enum value to get the string value for.</param>
	/// <param name="ignoreMissingValue">
	/// If true, returns null when no FieldStringValue attribute is found.
	/// If false, throws an ArgumentException when no attribute is found.
	/// </param>
	/// <returns>
	/// The string value from the FieldStringValue attribute, or null if ignoreMissingValue is true and no attribute is found.
	/// </returns>
	/// <exception cref="ArgumentException">
	/// Thrown when ignoreMissingValue is false and no FieldStringValue attribute is found on the enum field.
	/// </exception>
	/// <example>
	/// <code>
	/// MyEnum value = MyEnum.First;
	/// string stringValue = value.GetFieldStringValue(); // Returns "First Option"
	/// </code>
	/// </example>
	public static string GetFieldStringValue(this Enum value, bool ignoreMissingValue = false)
	{
		var field = value.GetType().GetField(value.ToString());
		var attr = field?.GetCustomAttribute<FieldStringValue>();
        
		if (attr == null)
		{
			return ignoreMissingValue ? null : throw new ArgumentException($"No FieldStringValue attribute found for {value}");
		}
    
		return attr.Value;    
	}
}