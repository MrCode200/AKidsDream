#nullable enable

using System;

namespace AKidsDream.Common.Errors;

public interface IGameError
{
    string Code { get; }
    string Message { get; }
}

public abstract record GameError(string Code, string Message) : IGameError
{
    public override string ToString() => $"[{Code}] {Message}";
}

public record UnexpectedError(Exception Error) : GameError("UNEXPECTED_ERROR", $"Unexpected error: {Error}");