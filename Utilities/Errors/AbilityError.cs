using AKidsDream.Util.Identifiers;
using Godot;

namespace AKidsDream.Common.Errors;

public abstract record AbilityError(
    string Code,
    string Message,
    IIdTag CasterId,
    string AbilityName
) : GameError(Code, Message)
{
    public sealed record InvalidTargetCount(
        IIdTag CasterId,
        string AbilityName,
        int Min,
        int Max,
        int Actual
    ) : AbilityError(
        "ABILITY.INVALID_TARGET_COUNT",
        $"Expected between {Min} and {Max} targets, got {Actual}.",
        CasterId,
        AbilityName
    );

    public sealed record MaxDuplicateTargetsExceeded(
        IIdTag CasterId,
        string AbilityName,
        Vector2I Tile,
        int MaxAllowed,
        int ActualCount
    ) : AbilityError(
        "ABILITY.MAX_DUPLICATE_TARGETS_EXCEEDED",
        $"Tile {Tile} was selected {ActualCount} times, exceeding the maximum of {MaxAllowed}.",
        CasterId,
        AbilityName
    );

    public sealed record TargetOutOfRange(
        IIdTag CasterId,
        string AbilityName,
        Vector2I Tile,
        Vector2I? Origin
    ) : AbilityError(
        "ABILITY.TARGET_OUT_OF_RANGE",
        $"Target tile {Tile} is out of range from origin {Origin}.",
        CasterId,
        AbilityName
    );

    public sealed record CannotAfford(
        IIdTag CasterId,
        string AbilityName,
        string PoolName,
        int Cost,
        int? Available
    ) : AbilityError(
        "ABILITY.CANNOT_AFFORD",
        Available is not null 
            ? $"Ability costs {Cost} {PoolName}, but only {Available} is available." 
            : $"Ability costs {Cost} {PoolName}, but no {PoolName} is available.",
        CasterId,
        AbilityName
    );
    
    public sealed record InvalidState(
        IIdTag CasterId,
        string AbilityName
    ) : AbilityError(
        "ABILITY.INVALID_STATE",
        $"The ability {AbilityName}'s state is invalid.",
        CasterId,
        AbilityName
    );

    public sealed record AbilityNotFound(
        IIdTag CasterId,
        string AbilityName
    ) : AbilityError(
        "ABILITY.ABILITY_NOT_FOUND",
        $"Ability {AbilityName} not found.",
        CasterId,
        AbilityName
    );
    
    /*
    public sealed record EffectFailed(
        IIdTag CasterId,
        string AbilityName,
        GameError Error
    ) : AbilityError(
        "ABILITY.EFFECT_FAILED",
        $"An effect failed: {Error.Message}",
        CasterId,
        AbilityName
    );*/
    
    public override string ToString() =>
        $"[{Code} | {CasterId} casting {AbilityName}] {Message}";
}

