using System;
using AKidsDream.Util.Identifiers;

namespace AKidsDream.Common.Errors;

public abstract record ValidationError(string Code, string Message) : GameError(Code, Message)
{
    public sealed record InvalidArgument(string ArgumentName, string Message) : ValidationError(
        "VALIDATION.INVALID_ARGUMENT",
        $"Invalid argument '{ArgumentName}': {Message}"
    );

    public sealed record NotPlayerTurn(IIdTag ActivePlayerId, IIdTag RequestedPlayerId, string FailedActionDescription) : ValidationError(
        "VALIDATION.NOT_PLAYER_TURN",
        $"{FailedActionDescription} cannot be performed by {RequestedPlayerId}. Active player is {ActivePlayerId}"
    );
}
