using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AKidsDream.Common.Logging;
using AKidsDream.Managers.SaveSystems;
using AKidsDream.Units.Resources;
using Serilog;

public partial class AnimationComponent : Node
{
	[Export] public Unit Unit;
	[Export] public AnimatedSprite2D Animator;
	public Global.UnitColor UnitColor;
	
	private ILogger _log = GameLogger.For<AnimationComponent>();
	
	private int _currentFrame = -1;
	private int _loopCount;
	private readonly HashSet<int> _reachedFrames = [];
	private StringName _currentAnimation;

	public override void _Ready()
	{
		_log = _log.ForContext("UnitName", Unit?.UnitName)
			.ForContext("UnitId", Unit?.UnitId);
		
		if (Animator != null)
		{
			Animator.AnimationLooped += OnAnimationLooped;
			Animator.AnimationFinished += OnAnimationFinished;
		}
	}

	public override void _Process(double delta)
	{
		if (Animator == null || !Animator.IsPlaying())
			return;
		
		var newFrame = Animator.Frame;
		if (newFrame != _currentFrame)
		{
			_currentFrame = newFrame;
			_reachedFrames.Add(_currentFrame);
		}
	}

	public void PlayAnimation(StringName animationName)
	{
		var animationString = $"{UnitColor}{Unit.UnitName}{animationName}";
		if (!Animator.SpriteFrames.HasAnimation(animationString))
		{
			Log.ForContext<AnimationComponent>().Here().Warn("Animation not found {AnimationString}", animationString);
			return;
		}
		
		Animator.Stop();
		ResetTracking();
		_currentAnimation = animationString;
		Animator.Play(animationString);
	}

	public void StopAnimation()
	{
		Animator.Stop();
		ResetTracking();
	}
	
	// -- UTIL MEHTODS --
	
	public int GetCurrentFrame() => _currentFrame;
	
	public int GetLoopCount() => _loopCount;
	
	public StringName CurrentAnimation() => _currentAnimation;
	
	public bool HasReachedFrame(int frame) => _reachedFrames.Contains(frame);
	
	public int GetAnimationFrameCount()
	{
		if (Animator?.SpriteFrames == null || _currentAnimation == null)
			return 0;
		return Animator.SpriteFrames.GetFrameCount(_currentAnimation);
	} 

	private void ResetTracking()
	{
		_currentFrame = -1;
		_loopCount = 0;
		_reachedFrames.Clear();
		_currentAnimation = new StringName("");
	}
	
	// -- WAIT METHODS --
	
	/// <summary>
	/// Waits until the animation reaches a specific frame.
	/// </summary>
	/// <param name="targetFrame">The frame number to wait for.</param>
	public async Task WaitForTargetFrame(int targetFrame)
	{
		while (Animator.IsPlaying() && _currentFrame != targetFrame)
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
	}

	public async Task WaitForTargetLoop(int targetLoop)
	{
		while (Animator.IsPlaying() && _loopCount < targetLoop)
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
	}

	/// <summary>
	/// Waits until the animation has looped a specific number of times.
	/// </summary>
	/// <param name="loopCount">The target loop count to wait for.</param>
	public async Task WaitForLoopCount(int loopCount)
	{
		while (Animator.IsPlaying() && _loopCount < loopCount)
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
	}
	
	/// <summary>
	/// Waits for a specific number of frames to pass from the current frame.
	/// </summary>
	/// <param name="frameCount">The number of frames to wait for.</param>
	public async Task WaitForFrames(int frameCount)
	{
		var targetFrame = (_currentFrame + frameCount) % GetAnimationFrameCount();
		var targetLoops = (_currentFrame + frameCount) / GetAnimationFrameCount();
		
		while (Animator.IsPlaying())
		{
			if (_loopCount < targetLoops && _currentFrame < targetFrame)
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
	}

	// -- ON SIGNALS --
	
	private void OnAnimationLooped()
	{
		_loopCount++;
		_reachedFrames.Clear();
	}

	private void OnAnimationFinished()
	{
		_loopCount++;
		_log.Here().Debug("Animation finished. Loop count: {LoopCount}", _loopCount);
	}
	
}
