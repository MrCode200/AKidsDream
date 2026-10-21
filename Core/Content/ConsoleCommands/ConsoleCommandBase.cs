#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using AKidsDream.Commands;
using AKidsDream.Utilities.Logging;
using AKidsDream.Utilities.Registry.TypeExtensions;
using Godot;
using Serilog;

namespace AKidsDream.Util.Identifiers.Commands;

public abstract partial class ConsoleCommandBase : GodotObject, IConsoleCommand
{
    private static readonly ILogger Log = GameLogger.For(typeof(ConsoleCommandBase));

    public abstract void Init(GameContext context);
    public abstract void Exit();
    
    // --- Print Methods ---
    private static void PrintError(string raw, Type finalType, string error)
    {
        Console.PrintError(error);
        Log.Here().Err("Parsing of '{RawInput}' to type {TargetType} failed: {ErrorMessage}",
            raw, finalType, error);
    }

    private static void PrintError(string raw, Type finalType, IEnumerable<string> errors)
    {
        foreach (var err in errors)
            PrintError(raw, finalType, err);
    }
    
    // --- Validation Methods ---

    protected static bool StringIn(string raw, string[] options)
    {
        if (options.Contains(raw))
            return true;
        PrintError(raw, typeof(string), $"'{raw}' is not a valid option. Valid options are: {string.Join(", ", options)}");
        return false;
    }

    protected static bool TryInt(
        string raw,
        string name,
        out int result,
        int? min = null,
        int? max = null,
        int[]? allowedValues = null
    )
    {
        if (!ArgParser.TryInt(raw, name, out result, out var parseErr))
        {
            PrintError(raw, typeof(int), parseErr);
            return false;
        }

        if (!ArgValidator.ValidateInt(result, out var validationErrors, min, max, allowedValues))
        {
            PrintError(raw, typeof(int), validationErrors);
            return false;
        }

        return true;
    }

    protected static bool TryEnum<TEnum>(string raw, string name, out TEnum result)
        where TEnum : struct
    {
        if (ArgParser.TryEnum(raw, name, out result, out var errMsg)) return true;
        PrintError(raw, typeof(TEnum), errMsg);
        return false;
    }
}