using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AKidsDream.Common.Logging;
using AKidsDream.Managers.SaveSystems;
using AKidsDream.Common;
using Serilog;

[GlobalClass]
[Icon("res://Assets/NodeIcons/animation.svg")]
public partial class AnimationComponent : Node
{
    [Export] public Unit Unit;
    [Export] public AnimatedSprite2D Animator;
    [Export] public StringName DefaultAnimation = "Idle";

    public Global.UnitColor UnitColor;

    private ILogger _log = GameLogger.For<AnimationComponent>();

    private readonly HashSet<int> _reachedFrames = [];
    private StringName _activeAnimation;
    private int _currentFrame = -1;
    private int _loopCount;

    /*
    public StringName _activeAnimation
    {
        get => _activeAnimation;
        set
        {
            _activeAnimation = value;
            EmitSignal(SignalName.TrackingUpdated);
        }
    }

    private int _currentFrame
    {
        get => _currentFrame;
        set
        {
            if (_currentFrame == value) return;
            _currentFrame = value;
            EmitSignal(SignalName.TrackingUpdated);
        }
    }

    private int _loopCount
    {
        get => _loopCount;
        set
        {
            _loopCount = value;
            EmitSignal(SignalName.TrackingUpdated);
        }
    }

        [Signal]
    public delegate void TrackingUpdatedEventHandler();
    */


    public override void _Ready()
    {
        _log = _log.ForContext("UnitName", Unit?.UnitName)
            .ForContext("UnitId", Unit?.UnitId);


        if (Animator == null) return;

        Animator.AnimationLooped += OnAnimationLooped;
        Animator.AnimationFinished += OnAnimationFinished;
        Animator.FrameChanged += OnFrameChanged;
        Animator.AnimationChanged += OnAnimationChanged;

        var defaultAnimationName = CreateAnimationString(DefaultAnimation);

        if (!string.IsNullOrEmpty(defaultAnimationName) && !Animator.SpriteFrames.HasAnimation(defaultAnimationName))
        {
            _log.Here().Warn("Default animation '{DefaultAnimation}' not found in sprite frames", defaultAnimationName);
            return;
        }

        PlayAnimation(DefaultAnimation);
    }

    private StringName CreateAnimationString(StringName animationName)
    {
        return $"{UnitColor}{animationName}";
    }

    public void PlayAnimation(StringName animationName)
    {
        var animationString = CreateAnimationString(animationName);
        if (!Animator.SpriteFrames.HasAnimation(animationString))
        {
            Log.ForContext<AnimationComponent>().Here().Warn("Animation not found {AnimationString}", animationString);
            return;
        }

        Animator.Stop();
        ResetTracking();
        _activeAnimation = animationString;
        _log.Here().Debug("Playing animation {AnimationString}", animationString);
        Animator.Play(animationString);
    }

    public void StopAnimation()
    {
        Animator.Stop();
        ResetTracking();
        TryPlayDefaultAnimation();
    }

// -- UTIL MEHTODS --

    public int GetCurrentFrame() => _currentFrame;

    public int GetLoopCount() => _loopCount;

    public StringName GetCurrentAnimation() => _activeAnimation;

    public bool HasReachedFrame(int frame) => _reachedFrames.Contains(frame);

    public int GetAnimationFrameCount()
    {
        if (Animator?.SpriteFrames == null || _activeAnimation == null)
            return 0;
        return Animator.SpriteFrames.GetFrameCount(_activeAnimation);
    }

    private void ResetTracking()
    {
        _currentFrame = -1;
        _loopCount = 0;
        _reachedFrames.Clear();
        _activeAnimation = new StringName("");
    }

    private void TryPlayDefaultAnimation()
    {
        if (!string.IsNullOrEmpty(DefaultAnimation))
        {
            PlayAnimation(DefaultAnimation);
        }
    }

// -- WAIT METHODS --

    /// <summary>
    /// Waits until the animation reaches a specific frame.
    /// </summary>
    /// <param name="targetFrame">The frame number to wait for.</param>
    public async Task WaitForTargetFrame(int targetFrame)
    {
        var targetAnimation = _activeAnimation;
        while (Animator.IsPlaying() && targetAnimation == _activeAnimation && _currentFrame != targetFrame)
            await ToSignal(EventBus.Instance, EventBus.SignalName.CallDeferredReached);
    }

    public async Task WaitForTargetLoop(int targetLoop)
    {
        var targetAnimation = _activeAnimation;
        while (Animator.IsPlaying() && targetAnimation == _activeAnimation && _loopCount < targetLoop)
            await ToSignal(EventBus.Instance, EventBus.SignalName.CallDeferredReached);
    }

    /// <summary>
    /// Waits until the animation has looped a specific number of times.
    /// </summary>
    /// <param name="loopCount">The target loop count to wait for.</param>
    public async Task WaitForLoopCount(int loopCount)
    {
        await WaitForFrames(loopCount * GetAnimationFrameCount());
    }

    /// <summary>
    /// Waits for a specific number of frames to pass from the current frame.
    /// </summary>
    /// <param name="frameCount">The number of frames to wait for.</param>
    public async Task WaitForFrames(int frameCount)
    {
        var currentFrame = (_currentFrame >= 0) ? _currentFrame : 0;
        var targetFrame = (currentFrame + frameCount) % GetAnimationFrameCount();
        var targetLoops = (currentFrame + frameCount) / GetAnimationFrameCount();

        var targetAnimation = _activeAnimation;
        while (
            Animator.IsPlaying()
            && targetAnimation == _activeAnimation
            && ( // While Loop count hasn't been reached, don't yet check for frames
                _loopCount < targetLoops ||
                _loopCount == targetLoops && _currentFrame < targetFrame
            )
        )
            await ToSignal(EventBus.Instance, EventBus.SignalName.CallDeferredReached);
    }

// -- ON SIGNALS --

    private void OnFrameChanged()
    {
        var frame = Animator.Frame;
        if (frame == _currentFrame)
            return;

        _currentFrame = frame;
        _reachedFrames.Add(_currentFrame);
    }

    private void OnAnimationChanged()
    {
        ResetTracking();
        _activeAnimation = Animator.Animation;
    }

    private void OnAnimationLooped()
    {
        _loopCount++;
        _reachedFrames.Clear();
    }

    private void OnAnimationFinished()
    {
        _loopCount++;
        _log.Here().Debug("Animation finished. Loop count: {LoopCount}", _loopCount);
        TryPlayDefaultAnimation();
    }
}