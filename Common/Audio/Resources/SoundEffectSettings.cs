#nullable enable
using AKidsDream.Common.Logging;
using Godot;
using Serilog;

namespace AKidsDream.Core.Managers.Audio;

public enum SoundEffectType
{
    UnassignedSound,
    ButtonClick,
    CardSelected,
    UnitSelected,
    TileSelected,
    SwordSlash,
    TimmyUwUAttack
}

public enum AudioBus
{
    Master,
    Music,
    SFX,
    UI,
}

[GlobalClass]
[Tool]
public partial class SoundEffectSettings : Resource
{
    [Export] public SoundEffectType Type = SoundEffectType.UnassignedSound;

    [ExportGroup("Basic Settings")] private bool _playPreview;

    [Export]
    public bool PlayPreview
    {
        get => _playPreview;
        set
        {
            if (_playPreview == value) return;
            _playPreview = value;
            if (value) _PlayPreviewSound();
            else _StopPreview();
        }
    }

    [Export] public AudioStream SoundEffect = null!;

    [Export(PropertyHint.Range, "-1, 10, or_greater")]
    public int Limit = -1;

    [Export(PropertyHint.Range, "-80, 6, 0.1")]
    public float Volume; //  Volume in decibels. 0 = default, negative = quieter, positive = louder.

    [Export] public float VolumeRandomness = 0.0f;

    [Export(PropertyHint.Range, "0.1, 4.0, 0.01")]
    public float PitchScale = 1.0f; // Base pitch. 1.0 = normal, 0.5 = half speed, 2.0 = double speed.

    [Export(PropertyHint.Range, "0, 1, 0.01")]
    public float PitchRandomness = 0.0f;

    [Export] public AudioBus Bus = AudioBus.Master;
    public string BusName => Bus.ToString();

    [ExportGroup("Audio 2D Settings")] [Export]
    public float MaxDistance = 2000f;

    [Export]
    public float Attenuation = 1.0f; // How fast volume drops with distance. 1.0 = linear, 2.0 = faster, 0.5 = slower

    [Export(PropertyHint.Range, "0, 4, 0.01")]
    public float
        PanningStrength = 1.0f; // How much left/right panning based on position. 0 = centered, 1 = normal stereo 

    [ExportGroup("Advanced Settings")]
    [Export] public bool DebugLog;

    // ---------------------------------------- INTERNAL AUDIO LIMIT MANAGEMENT ----------------------------------
    public int ActiveAudioCount;

    public bool HasOpenLimit => Limit <= -1 || ActiveAudioCount < Limit;

    public void OnAudioFinished() => ActiveAudioCount--;

    // -----------------------------------------------  HELPER METHODS ------------------------------------------
    public float GetRandomizedPitchScale(float? pitchScale = null)
    {
        pitchScale ??= PitchScale;
        if (PitchRandomness == 0.0f) return pitchScale.Value;

        var rng = new RandomNumberGenerator();
        return Mathf.Max(0.01f, pitchScale.Value + rng.RandfRange(-PitchRandomness, PitchRandomness));
    }

    public float GetRandomizedVolume(float? volume = null)
    {
        volume ??= Volume;
        if (VolumeRandomness == 0.0f) return volume.Value;

        var rng = new RandomNumberGenerator();
        return Mathf.Min(10, volume.Value + rng.RandfRange(-VolumeRandomness, VolumeRandomness));
    }

    // ------------------------------------------------ PREVIEW PLAYER -------------------------------------------
    private AudioStreamPlayer? _previewPlayer;

    private AudioStreamPlayer _CreatePreviewPlayer()
    {
        var player = new AudioStreamPlayer();
        player.Stream = SoundEffect;
        player.VolumeDb = GetRandomizedVolume();
        player.PitchScale = GetRandomizedPitchScale();
        player.Bus = Bus.ToString();
        return player;
    }

    private void _PlayPreviewSound()
    {
        if (SoundEffect is null)
        {
            OS.Alert("Cannot preview: no audio stream assigned", "Audio Preview Error");
            return;
        }

        if (Engine.GetSingleton("EditorInterface") is null)
        {
            OS.Alert("Cannot preview: EditorInterface not found", "Audio Preview Error");
            return;
        }

        ;

        _StopPreview();
        _previewPlayer = _CreatePreviewPlayer();
        var editorInterface = (EditorInterface)Engine.GetSingleton("EditorInterface");
        editorInterface.GetEditorMainScreen().AddChild(_previewPlayer);
        _previewPlayer.Finished += _OnPreviewFinished;
        _previewPlayer.Play();
    }

    private void _StopPreview()
    {
        if (_previewPlayer is null
            || !IsInstanceValid(_previewPlayer)
            || _previewPlayer.IsQueuedForDeletion()) return;

        _previewPlayer.Stop();
        _previewPlayer.QueueFree();
        _previewPlayer = null;
    }

    private void _OnPreviewFinished()
    {
        _previewPlayer?.Finished -= _OnPreviewFinished;
        _StopPreview();
        PlayPreview = false;
    }

    // -------------------------------------------------- LOGGING METHODS ---------------------------------------
    private static readonly ILogger Log = GameLogger.For(typeof(SoundEffectSettings));

    public void LogPlaying2D(Vector2 position, float actualVolume, float actualPitch)
    {
        var limitDisplay = Limit > 0 ? Limit : 999;
        Log.ForContext("Type", Type)
            .Here()
            .Debug("Playing 2D at position ({X:F1}, {Y:F1}) | bus: {Bus} | volume: {Volume:F1} dB | pitch: {Pitch:F2} | count: {Count}/{Limit}",
                position.X, position.Y, Bus, actualVolume, actualPitch, ActiveAudioCount, limitDisplay);
    }

    public void LogPlaying(float actualVolume, float actualPitch)
    {
        var limitDisplay = Limit > 0 ? Limit : 999;
        Log.ForContext("Type", Type)
            .Here()
            .Debug("Playing (non-positional) | bus: {Bus} | volume: {Volume:F1} dB | pitch: {Pitch:F2} | count: {Count}/{Limit}",
                Bus, actualVolume, actualPitch, ActiveAudioCount, limitDisplay);
    }

    public void LogLimitReached()
    {
        Log.ForContext("Type", Type)
            .Here()
            .Warn("LIMIT REACHED! Count: {Count}/{Limit}", ActiveAudioCount, Limit);
    }

    public void LogFinished()
    {
        var limitDisplay = Limit > 0 ? Limit : 999;
        Log.ForContext("Type", Type)
            .Here()
            .Debug("Finished & queued free | count: {Count}/{Limit}", ActiveAudioCount - 1, limitDisplay);
    }
}