#nullable enable
using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;

namespace AKidsDream.Common.Components.TweenComponent.Resources;

public enum TweenTrigger
{
    Manual,
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

public enum CurveMode
{
    None,
    Mirror
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

    [Export]
    public Array<TweenTrigger> Triggers
    {
        get => _triggers;
        set
        {
            _triggers = value;
            NotifyPropertyListChanged();
        }
    }

    [Export] public Array<CustomSignalData> CustomSignals = [];

    [ExportGroup("Looping")] private TweenLoopMode _loopMode = TweenLoopMode.None;

    [Export]
    public TweenLoopMode LoopMode
    {
        get => _loopMode;
        set
        {
            _loopMode = value;
            NotifyPropertyListChanged();
        }
    }

    [Export] public int MaxLoopCount = -1;
    [Export] public CurveMode CurveMode = CurveMode.Mirror;

    [ExportGroup("PlayBackBehaviour")] private TweenConflictPolicy _conflictPolicy = TweenConflictPolicy.Ignore;

    [Export]
    public TweenConflictPolicy ConflictPolicy
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

    [ExportGroup("Post-Finish")] [Export] public bool HideTargetOnFinish;
    [Export] public bool QueueFreeTargetOnFinish;

    [ExportGroup("")] [Export] public float DelayAnimStart;

    public static readonly HashSet<StringName> Identifiers = [];
    private StringName _identifier = "UniqueIdentifier";

    [Export]
    public StringName Identifier
    {
        get => _identifier;
        set
        {
            if (!Engine.IsEditorHint())
            {
                _identifier = value;
                return;
            }

            Identifiers.Remove(_identifier);
            _identifier = value;
            if (!Identifiers.Add(value))
                OS.Alert($"Duplicate identifier: {value}", "Warning");
        }
    }


    public override void _ValidateProperty(Dictionary property)
    {
        var propertyName = property["name"].AsString();

        var show = propertyName switch
        {
            nameof(CustomSignals) => Triggers.Contains(TweenTrigger.CustomSignal),
            nameof(MaxLoopCount) => LoopMode != TweenLoopMode.None,
            nameof(MaxQueueCount) => ConflictPolicy == TweenConflictPolicy.Queue,
            nameof(CurveMode) => LoopMode == TweenLoopMode.PingPong,

            _ => true
        };

        if (!show)
        {
            property["usage"] = (int)PropertyUsageFlags.NoEditor;
            return;
        }
        
        var (disable, hint) = propertyName switch
        {
            nameof(CurveMode) => (!Steps.Any(x => x.CustomCurve is not null),
                "To use CurveMode, at least 1 step must have a custom curve defined."),
            _ => (false, "")
        };

        if (!string.IsNullOrEmpty(hint))
            property["hint_text"] = hint;
        
        if (disable)
            property["usage"] = (int)(property["usage"].AsInt32() | (long)PropertyUsageFlags.ReadOnly);
    }
}