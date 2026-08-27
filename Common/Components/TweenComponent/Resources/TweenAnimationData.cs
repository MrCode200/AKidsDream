#nullable enable
using Godot;
using Godot.Collections;

namespace AKidsDream.Common.Components;

public enum TweenTrigger
{
    Ready,
    Show,
    Hide,
    MouseEnter,
    MouseExit,
    CustomSignal
}

public enum TweenLoopMode
{
    None, 
    Loop,
    PingPong,
    // ReverseOnRetrigger
}

public enum TweenConflictPolicy
{
    Ignore,
    Restart,
    Queue
}

[GlobalClass]
[Tool]
public partial class TweenAnimationData : Resource
{
    private Array<TweenTrigger> _triggers = [TweenTrigger.Ready];

    [Export] public Array<TweenStep> Steps = [new()];
    [Export] public Array<TweenTrigger> Triggers
    {
        get => _triggers;
        set
        {
            _triggers = value;
            NotifyPropertyListChanged();
        }
    }
    [Export] public Array<CustomSignalData> CustomSignals = [];

    [ExportGroup("Looping")]
    private TweenLoopMode _loopMode = TweenLoopMode.None;
    [Export] public TweenLoopMode LoopMode
    {
        get => _loopMode;
        set
        {
            _loopMode = value;
            NotifyPropertyListChanged();
        }
    }
    [Export] public int MaxLoopCount = -1;

    [ExportGroup("PlayBackBehaviour")]
    private TweenConflictPolicy _conflictPolicy = TweenConflictPolicy.Ignore;
    [Export] public TweenConflictPolicy ConflictPolicy
    {
        get => _conflictPolicy;
        set
        {
            _conflictPolicy = value;
            NotifyPropertyListChanged();
        }
    }
    [Export] public int MaxQueueCount = -1;
    [Export] public Tween.TweenPauseMode PauseMode = Tween.TweenPauseMode.Bound;
    
    [ExportGroup("Post-Finish")]
    [Export] public bool HideTargetOnFinish;
    [Export] public bool QueueFreeTargetOnFinish;
    
    [ExportGroup("")]
    [Export] public float DelayAnimStart = 0f;
    [Export] public StringName Identifier = "";

    public override void _ValidateProperty(Dictionary property)
    {
        var propertyName = property["name"].AsString();

        var (show, hint) = propertyName switch
        {
            nameof(CustomSignals) => (Triggers.Contains(TweenTrigger.CustomSignal),
                "Add 'CustomSignal' to Triggers array to use this"),
            nameof(MaxLoopCount) => (LoopMode != TweenLoopMode.None,
                "Set LoopMode to Loop or PingPong to use this"),
            nameof(MaxQueueCount) => (ConflictPolicy == TweenConflictPolicy.Queue,
                "Set ConflictPolicy to Queue to use this"),
            _ => (true, "")
        };

        if (!string.IsNullOrEmpty(hint))
            property["hint_text"] = hint;

        if (!show)
            property["usage"] = (int)PropertyUsageFlags.NoEditor;
    }
}