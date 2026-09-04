#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;

namespace AKidsDream.Util.Identifiers;

public static class ArgParser
{
    public static bool TryInt(string raw, string name,
        out int result,
        [NotNullWhen(false)] out string? errorMessage)
    {
        errorMessage = null;
        if (int.TryParse(raw, out result)) return true;
        errorMessage = $"Invalid {name}: must be an integer";
        return false;
    }

    public static bool TryEnum<TEnum>(string raw, string name,
        out TEnum result,
        [NotNullWhen(false)] out string? errorMessage)
        where TEnum : struct
    {
        errorMessage = null;
        if (
            Enum.TryParse(raw, true, out result) &&
            Enum.IsDefined(typeof(TEnum), result)
        ) return true;
        
        var validValues = string.Join(", ", Enum.GetNames(typeof(TEnum)));
        
        errorMessage = $"Invalid {name} '{raw}': must be one of {validValues}.";
        return false;
    }
}