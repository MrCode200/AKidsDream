using Godot;
using Godot.Collections;

namespace AKidsDream.Utilities.Registry.TypeExtensions;

public static class DictionaryExtensions
{
    public static Godot.Collections.Dictionary<TKey, TValue> ToGodotDictionary<[MustBeVariant]TKey, [MustBeVariant]TValue>(
        this System.Collections.Generic.Dictionary<TKey, TValue> dictionary)
    {
        var result = new Godot.Collections.Dictionary<TKey, TValue>();
        foreach (var (key, value) in dictionary)
        {
            result[key] = value;
        }
        return result;
    }
}