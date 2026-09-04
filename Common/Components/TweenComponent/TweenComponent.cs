#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using AKidsDream.Common.Logging;
using Godot;
using Godot.Collections;
using Serilog;

namespace AKidsDream.Common.Components.TweenComponent.Resources;

[GlobalClass]
[Icon("res://Common/Components/TweenComponent/animate.png")]
[Tool]
public partial class TweenComponent : Node
{
    [Export] public required CanvasItem Target = null!;
    [Export] public required Array<TweenAnimationData> TweenDatas = [];
    [Export] public bool DisableTween;

    public Tween? Tween;

    private TweenAnimationData? _activeData;
    private readonly System.Collections.Generic.Dictionary<TweenAnimationData, Node[]> _resolvedStepTargets = new();
    private readonly System.Collections.Generic.Dictionary<TweenAnimationData, Variant[]> _originalValues = new();
    private readonly List<Action> _unsubscribers = new();
    private readonly List<Action> _runtimeUnsubscribers = new();

    private int _queuedReplay;
    private bool _hasQueuedReplaySubscription;
    private bool _hasFinishedSubscription;
    private bool _isRunningOnHide;
    private TaskCompletionSource<bool>? _tweenCompletion;

    private static readonly ILogger Log = GameLogger.For<TweenComponent>();

    [ExportToolButton("Play")]
    private Callable Play => Callable.From(() =>
    {
        if (!_ValidateIdentifiers())
            return;

        ResolveAllStepTargets();
        foreach (var data in TweenDatas)
        {
            if (!_originalValues.ContainsKey(data))
                StoreOriginalValues(data);
        }

        foreach (var data in TweenDatas)
            ResetToOriginalValues(data);

        _ = PlayAllAnimationsAsync();
    });

    private async Task PlayAllAnimationsAsync()
    {
        foreach (var data in TweenDatas)
        {
            await RunAnimationAsync(data);
        }
    }

    [ExportToolButton("Stop/Reset")]
    public Callable Stop => Callable.From(() =>
    {
        Tween?.Kill();
        if (_resolvedStepTargets.Count == 0)
            ResolveAllStepTargets();

        foreach (var data in TweenDatas)
            ResetToOriginalValues(data);
    });

    private void ResolveAllStepTargets()
    {
        foreach (var data in TweenDatas)
            _resolvedStepTargets[data] = ResolveStepTargets(data);
    }

    private Node[] ResolveStepTargets(TweenAnimationData data)
    {
        var resolved = new Node[data.Steps.Count];
        for (var i = 0; i < data.Steps.Count; i++)
        {
            var step = data.Steps[i];
            var target = string.IsNullOrEmpty(step.TargetOverride)
                ? Target
                : GetNodeOrNull<Node>(step.TargetOverride);

            if (target == null)
            {
                Log.Here().Warn("Target not found, NodePath: {TargetOverride} for {Property}, using default Target",
                    step.TargetOverride, step.Property);
                target = Target;
            }

            resolved[i] = target;
        }

        return resolved;
    }

    private void StoreOriginalValues(TweenAnimationData data)
    {
        var targets = _resolvedStepTargets[data];
        var values = new Variant[data.Steps.Count];

        for (var i = 0; i < data.Steps.Count; i++)
            values[i] = targets[i].Get(new StringName(data.Steps[i].Property));


        _originalValues[data] = values;
    }


    private void ResetToOriginalValues(TweenAnimationData data)
    {
        if (!_originalValues.TryGetValue(data, out var values))
            return;

        var targets = _resolvedStepTargets[data];
        for (var i = 0; i < data.Steps.Count; i++)
            targets[i].Set(new StringName(data.Steps[i].Property), values[i]);
    }


    public override void _Ready()
    {
        ResolveAllStepTargets();

        if (!_ValidateTriggers())
            return;

        if (!_ValidateProperties())
            return;

        if (!_ValidateIdentifiers())
            return;

        if (Engine.IsEditorHint() || DisableTween)
            return;

        foreach (var data in TweenDatas)
        {
            StoreOriginalValues(data);
            SubToTriggerEvents(data);
        }
    }

    public override void _ExitTree()
    {
        if (Engine.IsEditorHint() || DisableTween)
            return;

        foreach (var unsub in _unsubscribers)
            unsub();
        _unsubscribers.Clear();

        foreach (var unsub in _runtimeUnsubscribers)
            unsub();
        _runtimeUnsubscribers.Clear();
    }

    private void SubToTriggerEvents(TweenAnimationData data)
    {
        void RunAnimationWrapper() => RunAnimation(data);

        if (data.Triggers.Contains(TweenTrigger.Ready))
        {
            if (!Target.IsAncestorOf(this))
                Log.Here().Warn("Target is not a child of this node, while Trigger contains Ready");

            Target.Ready += RunAnimationWrapper;
            _unsubscribers.Add(() => Target.Ready -= RunAnimationWrapper);
        }

        if (data.Triggers.Contains(TweenTrigger.Hide))
        {
            Log.Here().Warn(
                "Subscribing to Hidden event may contain bugs (especially if subscribing to Show at the same time(?))");
            void RunOnHideWrapper() => RunOnHide(data);
            Target.Hidden += RunOnHideWrapper;
            _unsubscribers.Add(() => Target.Hidden -= RunOnHideWrapper);
        }

        if (data.Triggers.Contains(TweenTrigger.Show))
        {
            void RunOnShowWrapper() => RunOnShow(data);
            Target.VisibilityChanged += RunOnShowWrapper;
            _unsubscribers.Add(() => Target.VisibilityChanged -= RunOnShowWrapper);
        }

        if (data.Triggers.Contains(TweenTrigger.MouseEnter))
        {
            switch (Target)
            {
                case CollisionObject2D co:
                    co.MouseEntered += RunAnimationWrapper;
                    _unsubscribers.Add(() => co.MouseEntered -= RunAnimationWrapper);
                    break;
                case Control c:
                    c.MouseEntered += RunAnimationWrapper;
                    _unsubscribers.Add(() => c.MouseEntered -= RunAnimationWrapper);
                    break;
                default:
                    Log.Here().Warn("Target type {Type} does not support MouseEnter", Target.GetType());
                    break;
            }
        }

        if (data.Triggers.Contains(TweenTrigger.MouseExit))
        {
            switch (Target)
            {
                case CollisionObject2D co:
                    co.MouseExited += RunAnimationWrapper;
                    _unsubscribers.Add(() => co.MouseExited -= RunAnimationWrapper);
                    break;
                case Control c:
                    c.MouseExited += RunAnimationWrapper;
                    _unsubscribers.Add(() => c.MouseExited -= RunAnimationWrapper);
                    break;
                default:
                    Log.Here().Warn("Target type {Type} does not support MouseExit", Target.GetType());
                    break;
            }
        }

        if (data.Triggers.Contains(TweenTrigger.CustomSignal))
        {
            foreach (var signalData in data.CustomSignals)
            {
                var emitter = signalData.EmitterPath == null
                    ? Target
                    : GetNodeOrNull<Node>(signalData.EmitterPath);

                if (emitter == null)
                {
                    Log.Here().Warn("CustomSignalEmitter not found NodePath: {EmitterPath}", signalData.EmitterPath);
                    continue;
                }

                if (!emitter.HasSignal(signalData.CustomSignalName))
                {
                    Log.Here().Warn("CustomSignalEmitter doesn't contain {SignalName}", signalData.CustomSignalName);
                    continue;
                }

                signalData.Emitter = emitter;
                var callable = Callable.From(RunAnimationWrapper);
                emitter.Connect(signalData.CustomSignalName, callable);
                _unsubscribers.Add(() => emitter.Disconnect(signalData.CustomSignalName, callable));
            }
        }
    }

    private bool _ValidateTriggers()
    {
        var seen = new HashSet<TweenTrigger>();

        foreach (var trigger in TweenDatas.SelectMany(x => x.Triggers))
        {
            if (seen.Add(trigger)) continue;
            Log.Here().Warn("Duplicate triggers found");
            return false;
        }

        return true;
    }

    private bool _ValidateProperties()
    {
        return true;
        foreach (var data in TweenDatas)
        {
            for (var i = 0; i < data.Steps.Count; i++)
            {
                var step = data.Steps[i];
                if (step.Property.IsEmpty) continue;

                var stepTarget = _resolvedStepTargets[data][i];
                if (HasProperty(stepTarget, step.Property))
                    continue;

                Log.Here().Warn(
                    $"Disabled TweenComponent => Property '{step.Property}' not found on target '{stepTarget.Name}':'{stepTarget}'");
                return false;
            }
        }

        return true;
    }

    static bool HasProperty(Node node, string name)
    {
        if (node.Get(name).VariantType != Variant.Type.Nil) return true;

        var list = node.GetPropertyList();
        return list.Any(p => p["name"].AsString() == name);
    }

    private bool _ValidateIdentifiers()
    {
        var seen = new HashSet<StringName>();

        foreach (var data in TweenDatas)
        {
            if (string.IsNullOrEmpty(data.Identifier))
            {
                Log.Here().Warn("Animation data has empty Identification");
                return false;
            }

            if (!seen.Add(data.Identifier))
            {
                Log.Here().Warn("Duplicate Identification found: {Identification}", data.Identifier);
                return false;
            }
        }

        return true;
    }

    public TweenAnimationData? GetAnimationDataByIdentifier(StringName identifier)
    {
        return TweenDatas.FirstOrDefault(data => data.Identifier == identifier);
    }

    private void RunOnShow(TweenAnimationData data)
    {
        if (_isRunningOnHide)
        {
            return;
        }

        if (Target.IsVisible())
            RunAnimation(data);
    }

    private async void RunOnHide(TweenAnimationData data)
    {
        if (_isRunningOnHide)
            return;
        RunAnimation(data);
        /*
        _isRunningOnHide = true;

        try
        {
            Target.Visible = true;

            await RunAnimationAsync(data);

            if (_isRunningOnHide)
                Target.Visible = false;
        }
        catch (Exception e)
        {
            Log.Here().Err(e, "Exception in RunOnHide");
        }
        finally
        {
            _isRunningOnHide = false;
        }*/
    }

    public async Task RunAnimationAsync(StringName identifier)
    {
        var data = GetAnimationDataByIdentifier(identifier);
        if (data == null)
        {
            Log.Here().Warn("Animation data not found for identifier: {Identifier}", identifier);
            return;
        }

        await RunAnimationAsync(data);
    }

    public async Task RunAnimationAsync(TweenAnimationData data)
    {
        try
        {
            if (DisableTween)
                return;

            RunAnimation(data);

            if (_tweenCompletion != null)
                await _tweenCompletion.Task;
        }
        catch (Exception e)
        {
            Log.Here().Err(e, "Exception in RunAnimationAsync");
        }
    }

    public void RunAnimation(StringName identifier)
    {
        var data = GetAnimationDataByIdentifier(identifier);
        if (data == null)
        {
            Log.Here().Warn("Animation data not found for identifier: {Identifier}", identifier);
            return;
        }

        RunAnimation(data);
    }

    public void RunAnimation(TweenAnimationData data)
    {
        if (DisableTween)
            return;

        if (Tween != null && Tween.IsValid() && Tween.IsRunning() && _activeData?.Identifier == data.Identifier)
        {
            switch (data.ConflictPolicy)
            {
                case TweenConflictPolicy.Ignore:
                    return;

                case TweenConflictPolicy.Queue:
                    if (data.MaxQueueCount > 0 && _queuedReplay >= data.MaxQueueCount)
                        return;
                    _queuedReplay += 1;

                    if (!_hasQueuedReplaySubscription)
                    {
                        void OnQueuedReplayWrapper() => OnQueuedReplay(data);
                        Tween.Finished += OnQueuedReplayWrapper;
                        _runtimeUnsubscribers.Add(() => Tween.Finished -= OnQueuedReplayWrapper);
                        _hasQueuedReplaySubscription = true;
                    }

                    return;

                case TweenConflictPolicy.Restart:
                default:
                    break;
            }
        }

        _tweenCompletion?.TrySetResult(false);
        _queuedReplay = 0;
        ClearRuntimeSubscribers();

        _ = BuildTween(data);
    }

    private async Task<Tween?> BuildTween(TweenAnimationData data)
    {
        if (DisableTween)
            return null!;

        _activeData = data;

        // Log.Here().Verbose("Building tween '{Identifier}' for {Target}", data.Identifier, Target);

        if (data.DelayAnimStart > 0)
        {
            await ToSignal(
                GetTree().CreateTimer(data.DelayAnimStart),
                SceneTreeTimer.SignalName.Timeout
            );

            if (_activeData != data)
                return null;
        }

        var targets = _resolvedStepTargets[data];

        Tween?.Kill();
        ClearRuntimeSubscribers();
        Tween = Target.CreateTween();
        Tween.SetPauseMode(data.PauseMode);

        // Build Tweeners
        var count = data.Steps.Count;
        var fromValues = new Variant[count];
        var toValues = new Variant[count];

        for (var i = 0; i < count; i++)
        {
            var step = data.Steps[i];
            if (step.Disable)
                continue;

            var stepTarget = targets[i];

            // Parallel running
            if (step.RunParallelWithPrevious && i > 0)
                Tween.SetParallel();
            else
                Tween.SetParallel(false);

            // TreeOrderAction
            if (step.TreeOrderAction != TreeOrderAction.None)
            {
                step.FromTreeIndex = stepTarget.GetIndex();
                Tween.TweenCallback(Callable.From(() =>
                {
                    switch (step.TreeOrderAction)
                    {
                        case TreeOrderAction.MoveToFront:
                            stepTarget.GetParent().MoveChild(stepTarget, -1);
                            break;

                        case TreeOrderAction.MoveToBack:
                            stepTarget.GetParent().MoveChild(stepTarget, 0);
                            break;

                        case TreeOrderAction.MoveToIndex:
                            stepTarget.GetParent().MoveChild(stepTarget, step.ToTreeIndex);
                            break;
                    }
                }));
            }

            // Check if property is set, if set to 
            if (step.Property.IsEmpty)
                continue;

            // Property values
            fromValues[i] = step.UseExplicitFrom
                ? step.FromValue
                : stepTarget.Get(new StringName(step.Property));
            toValues[i] = step.ToValue;

            var forward = Tween
                .TweenProperty(stepTarget, step.Property, step.ToValue, step.Duration)
                .SetTrans(step.Transition)
                .SetEase(step.Ease);

            if (step.UseExplicitFrom)
                forward.From(step.FromValue);
            if (step.Delay > 0)
                forward.SetDelay(step.Delay);
            if (step.SetValueRelative)
                forward.AsRelative();
        }

        // PingPong
        if (data.LoopMode == TweenLoopMode.PingPong)
        {
            for (var i = count - 1; i >= 0; i--)
            {
                var step = data.Steps[i];
                var stepTarget = targets[i];
                var prevStep = i < count - 1 ? data.Steps[i + 1] : null;

                // Parallel
                var wasParallelWithNext = prevStep?.RunParallelWithPrevious;
                if (wasParallelWithNext is not null)
                    Tween.SetParallel(wasParallelWithNext.Value);

                // TreeOrderAction
                if (step.TreeOrderAction != TreeOrderAction.None)
                {
                    Tween.TweenCallback(Callable.From(() =>
                    {
                        stepTarget.GetParent().MoveChild(stepTarget, step.FromTreeIndex);
                    }));
                }

                // Check if property is set, if set to 
                if (step.Property.IsEmpty)
                    continue;

                var newToValue = step.SetValueRelative ? Negate(toValues[i]) : fromValues[i];
                var backward = Tween.TweenProperty(stepTarget, step.Property, newToValue, step.Duration)
                    .SetTrans(step.Transition)
                    .SetEase(step.Ease);

                if (step.SetValueRelative)
                    backward.AsRelative();
                else
                    backward.From(toValues[i]);

                if (prevStep?.Delay > 0)
                    backward.SetDelay(prevStep.Delay);
            }

            Tween.SetParallel(false);
        }

        // LoopMode
        if (data.LoopMode != TweenLoopMode.None)
        {
            if (data.MaxLoopCount > 0)
                Tween.SetLoops(data.MaxLoopCount);
            else
                Tween.SetLoops();
        }

        // Finished event subscription
        if (!_hasFinishedSubscription)
        {
            _hasFinishedSubscription = true;
            _tweenCompletion = new TaskCompletionSource<bool>();

            void OnTweenFinishedWrapper()
            {
                _tweenCompletion?.TrySetResult(true);
                OnTweenFinished(data);
            }

            Tween.Finished += OnTweenFinishedWrapper;
            _runtimeUnsubscribers.Add(() => Tween.Finished -= OnTweenFinishedWrapper);
        }

        return Tween;
    }

    private static Variant Negate(Variant value)
    {
        return value.VariantType switch
        {
            Variant.Type.Int => Variant.From(-value.AsInt64()),
            Variant.Type.Float => Variant.From(-value.AsDouble()),
            Variant.Type.Vector2 => Variant.From(-value.AsVector2()),
            Variant.Type.Vector2I => Variant.From(-value.AsVector2I()),
            Variant.Type.Vector3 => Variant.From(-value.AsVector3()),
            Variant.Type.Vector3I => Variant.From(-value.AsVector3I()),
            Variant.Type.Color => Variant.From(-value.AsColor()),
            _ => UnsupportedNegateValue(value)
        };
    }

    private static Variant UnsupportedNegateValue(Variant value)
    {
        Log.Here().Err($"Negate is not supported for Variant type {value.VariantType}.");
        return value;
    }

    private void ClearRuntimeSubscribers()
    {
        foreach (var unsub in _runtimeUnsubscribers)
        {
            try
            {
                unsub();
            }
            catch (Exception e)
            {
                Log.Here().Err($"Failed to unsubscribe from event: {e}");
            }
        }

        _runtimeUnsubscribers.Clear();
        _hasQueuedReplaySubscription = false;
        _hasFinishedSubscription = false;
    }

    private void OnQueuedReplay(TweenAnimationData data)
    {
        if (_queuedReplay <= 0)
            return;

        _queuedReplay -= 1;

        if (_activeData != data)
            _ = BuildTween(data);
    }

    private void OnTweenFinished(TweenAnimationData data)
    {
        if (_activeData != data)
            return;

        if (data.QueueFreeTargetOnFinish)
        {
            Target.QueueFree();
            return;
        }

        if (data.HideTargetOnFinish)
            Target.Visible = false;
    }
}