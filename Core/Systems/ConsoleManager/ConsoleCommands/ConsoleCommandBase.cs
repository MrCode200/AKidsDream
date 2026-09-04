#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using AKidsDream.Commands;
using AKidsDream.Common.Logging;
using AKidsDream.Utilities.TypeExtensions;
using Godot;
using Serilog;

namespace AKidsDream.Util.Identifiers.Commands;

public abstract partial class ConsoleCommandBase : Node, IConsoleCommand
{
    protected GameContext? Context { get; private set; }

    public void SetContext(GameContext context) => Context = context;
    private static readonly ILogger Log = GameLogger.For(typeof(ConsoleCommandBase));

    [MemberNotNullWhen(true, nameof(Context))]
    protected bool RequireContext()
    {
        if (Context != null) return true;
        Console.PrintError("GameContext not initialized");
        return false;
    }

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
        if (ArgParser.TryEnum<TEnum>(raw, name, out result, out var errMsg)) return true;
        PrintError(raw, typeof(TEnum), errMsg);
        return false;
    }
}