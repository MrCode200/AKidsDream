namespace AKidsDream.Common.Errors;

public abstract record EffectError(string Code, string Message) : GameError(Code, Message)
{
    public sealed record InvalidTargetCount(int Min, int Max, int Actual)
        : EffectError("EFFECT.INVALID_TARGET_COUNT", $"Expected between {Min} and {Max} targets, but received {Actual}.");

    public sealed record ExecutionFailed(string Reason)
        : EffectError("EFFECT.EXECUTION_FAILED", $"Effect execution failed: {Reason}");
}
