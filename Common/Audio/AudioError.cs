using System;
using AKidsDream.Common.Errors;

namespace AKidsDream.Core.Managers.Audio;

public abstract record AudioError(string Code, string Message, SoundEffectType Type) : GameError(Code, Message)
{
    public sealed record SoundEffectNotFound(SoundEffectType Type)
        : AudioError("AUDIO.SOUND_EFFECT_TYPE_NOT_FOUND", $"Sound effect '{Type}' not found.", Type);

    public sealed record SoundEffectLimitReached(SoundEffectType Type, int Limit)
        : AudioError("AUDIO.SOUND_EFFECT_LIMIT_REACHED", $"Sound effect limit of '{Limit}' reached for type '{Type}'.",
            Type);
}