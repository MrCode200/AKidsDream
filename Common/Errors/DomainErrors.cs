#nullable enable
using System;
using Godot;

namespace AKidsDream.Common.Errors;

public abstract record EffectError(string Code, string Message) : GameError(Code, Message)
{
    public sealed record InvalidCaster(string Reason)
        : EffectError("EFFECT_INVALID_CASTER", $"Invalid caster: {Reason}");

    public sealed record InvalidTarget(string Reason)
        : EffectError("EFFECT_INVALID_TARGET", $"Invalid target: {Reason}");

    public sealed record NoAffectedTiles(string EffectName)
        : EffectError("EFFECT_NO_AFFECTED_TILES", $"Effect '{EffectName}' produced no affected tiles.");

    public sealed record ExecutionFailed(string Reason)
        : EffectError("EFFECT_EXECUTION_FAILED", $"Effect execution failed: {Reason}");

    public sealed record InvalidTargetCount(int Min, int Max, int Actual)
        : EffectError("EFFECT_INVALID_TARGET_COUNT", $"Expected between {Min} and {Max} targets, but received {Actual}.");
}

public abstract record CastError(string Code, string Message) : GameError(Code, Message)
{
    public sealed record AbilityNotFound(StringName AbilityName)
        : CastError("CAST_ABILITY_NOT_FOUND", $"Ability '{AbilityName}' was not found on caster.");

    public sealed record CannotAfford(string PoolName, int Required, int Available)
        : CastError("CAST_CANNOT_AFFORD", $"Requires {Required} {PoolName}, but only {Available} is available.");

    public sealed record TargetOutOfRange(Vector2I Tile, Vector2I? Origin)
        : CastError("CAST_TARGET_OUT_OF_RANGE", $"Tile {Tile} is out of reach from origin {Origin}.");

    public sealed record InvalidTargetCount(int Min, int Max, int Actual)
        : CastError("CAST_INVALID_TARGET_COUNT", $"Expected between {Min} and {Max} targets, got {Actual}.");

    public sealed record MaxDuplicateTargetsExceeded(Vector2I Tile, int MaxAllowed, int ActualCount)
        : CastError("CAST_MAX_DUPLICATE_TARGETS", $"Tile {Tile} was selected {ActualCount} times, exceeding max of {MaxAllowed}.");

    public sealed record EffectFailed(EffectError InnerError)
        : CastError("CAST_EFFECT_FAILED", $"Effect failed: {InnerError.Message}");
}

public abstract record CommandError(string Code, string Message) : GameError(Code, Message)
{
    public sealed record NullArgument(string ParamName, string Reason)
        : CommandError("CMD_NULL_ARGUMENT", $"Missing required argument '{ParamName}': {Reason}");

    public sealed record InvalidArgument(string ParamName, string Reason)
        : CommandError("CMD_INVALID_ARGUMENT", $"Invalid argument '{ParamName}': {Reason}");

    public sealed record NotPlayerTurn(PlayerId ActivePlayer, PlayerId AttemptedPlayer)
        : CommandError("CMD_NOT_PLAYER_TURN", $"Cannot act on player {AttemptedPlayer}'s turn; active player is {ActivePlayer}.");

    public sealed record AbilityNotFound(StringName AbilityName)
        : CommandError("CMD_ABILITY_NOT_FOUND", $"Ability '{AbilityName}' not found.");

    public sealed record CastFailed(CastError InnerError)
        : CommandError(InnerError.Code, InnerError.Message);
    public sealed record ExceptionOccurred(Exception Exception)
        : CommandError("CMD_EXCEPTION", $"Command threw unhandled exception: {Exception.Message}");
}
