#nullable enable
using System;
using System.Collections.Generic;

namespace AKidsDream.Utilities.TypeExtensions;

public static class ArgValidator
{
    public static bool ValidateInt(
        int value,
        out List<string> errors,
        int? min = null,
        int? max = null,
        int[]? allowedValues = null
    )
    {
        errors = [];

        if (value < min)
            errors.Add($"Value {value} must be >= {min}");

        if (value > max)
            errors.Add($"Value {value} must be < {max}");

        if (allowedValues is not null && !allowedValues.Contains(value))
            errors.Add($"Value {value} must be one of {string.Join(", ", allowedValues)}");

        return errors.Count == 0;
    }
}