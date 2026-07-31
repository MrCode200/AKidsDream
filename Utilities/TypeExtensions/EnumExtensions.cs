using System;
using System.Reflection;

namespace AKidsDream.Utilities.TypeExtensions;

/// <summary>
/// Attribute used to associate a typed value with enum fields.
/// This allows enum values to have additional metadata that can be retrieved via extension methods.
/// </summary>
/// <example>
/// <code>
/// public enum MyEnum
/// {
///     [FieldValue&lt;string&gt;("First Option")]
///     First,
///     
///     [FieldValue&lt;int&gt;(42)]
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