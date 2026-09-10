using AKidsDream.Common.Results;
using Godot;
using Godot.Collections;

namespace AKidsDream.Core.Managers.Audio;

public partial class AudioManager : Node
{
    [Export] public Array<SoundEffectSettings> SoundEffects = [new()];

    [ExportGroup("Fallback Settings")] [Export]
    public bool WarnOnMissingAudio = true;

    [Export] public bool FallbackToMasterOnMissingBuses = true;

    public static AudioManager Instance { get; private set; }

    public readonly Dictionary<SoundEffectType, SoundEffectSettings> SoundEffectTypeMap = new();
    private readonly Dictionary<SoundEffectType, AudioStream> _soundEffectAudioStreamsMap = new();

    public override void _Ready()
    {
        Instance = this;

        foreach (var soundEffect in SoundEffects)
        {
            if (soundEffect.Type == SoundEffectType.UnassignedSound)
                OS.Alert($"SoundEffectType {soundEffect.SoundEffect._GetStreamName()} is unassigned", "Unassigned SoundEffect");
            
            SoundEffectTypeMap.Add(soundEffect.Type, soundEffect);
        }
    }

    public override void _ExitTree()
    {
        Instance = null;
    }

    // -------------------------------------------- PLAY METHODS ------------------------------------------------------

    public Result<AudioError> PlayAudio(
        SoundEffectType type,
        float? volume = null,
        float? pitch = null
    )
    {
        if (!SoundEffectTypeMap.TryGetValue(type, out var soundEffect))
            return Result.Fail<AudioError>(new AudioError.SoundEffectNotFound(type));

        if (!soundEffect.HasOpenLimit)
        {
            if (soundEffect.DebugLog)
                soundEffect.LogLimitReached();
            return Result.Fail<AudioError>(new AudioError.SoundEffectLimitReached(type, soundEffect.Limit));
        }

        soundEffect.ActiveAudioCount++;
        var newPlayer = new AudioStreamPlayer();
        AddChild(newPlayer);

        newPlayer.Stream = soundEffect.SoundEffect;

        var actualVolume = soundEffect.GetRandomizedVolume(volume);
        var actualPitch = soundEffect.GetRandomizedPitchScale(pitch);
        newPlayer.VolumeDb = actualVolume;
        newPlayer.PitchScale = actualPitch;

        newPlayer.Bus = soundEffect.BusName;

        newPlayer.Finished += () =>
        {
            soundEffect.OnAudioFinished();
            if (soundEffect.DebugLog) soundEffect.LogFinished();
            newPlayer.QueueFree();
        };
        newPlayer.Play();
        if (soundEffect.DebugLog)
        {
            soundEffect.LogPlaying(actualVolume, actualPitch);
        }

        return Result.Ok<AudioError>();
    }

    public Result<AudioError> PlayAudioAtLocation(
        SoundEffectType type,
        Vector2 position,
        float? volume = null,
        float? pitch = null
    )
    {
        if (!SoundEffectTypeMap.TryGetValue(type, out var soundEffect))
            return Result.Fail<AudioError>(new AudioError.SoundEffectNotFound(type));

        if (!soundEffect.HasOpenLimit)
        {
            if (soundEffect.DebugLog) soundEffect.LogLimitReached();
            return Result.Fail<AudioError>(new AudioError.SoundEffectLimitReached(type, soundEffect.Limit));
        }

        soundEffect.ActiveAudioCount++;
        var newPlayer = new AudioStreamPlayer2D();
        AddChild(newPlayer);

        newPlayer.Stream = soundEffect.SoundEffect;

        var actualVolume = soundEffect.GetRandomizedVolume(volume);
        var actualPitch = soundEffect.GetRandomizedPitchScale(pitch);
        newPlayer.VolumeDb = actualVolume;
        newPlayer.PitchScale = actualPitch;

        newPlayer.Position = position;
        newPlayer.MaxDistance = soundEffect.MaxDistance;
        newPlayer.Attenuation = soundEffect.Attenuation;
        newPlayer.PanningStrength = soundEffect.PanningStrength;

        newPlayer.Bus = soundEffect.BusName;

        newPlayer.Finished += () =>
        {
            soundEffect.OnAudioFinished();
            if (soundEffect.DebugLog) soundEffect.LogFinished();
            newPlayer.QueueFree();
        };
        newPlayer.Play();
        if (soundEffect.DebugLog)
            soundEffect.LogPlaying2D(position, actualVolume, actualPitch);

        return Result.Ok<AudioError>();
    }

    // -------------------------------------------- HELPERS -----------------------------------------------------------
}