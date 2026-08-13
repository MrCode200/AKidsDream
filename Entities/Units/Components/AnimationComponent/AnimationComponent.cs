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
	private int _loopCount = 0;
	private HashSet<int> _reachedFrames = new();
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
	
	public bool HasReachedFrame(int frame) => _reachedFrames.Contains(frame);
	
	public bool HasLooped() => _loopCount > 0;
	
	public int GetAnimationFrameCount()
	{
		if (Animator?.SpriteFrames == null || _currentAnimation == null)
			return 0;
		return Animator.SpriteFrames.GetFrameCount(_currentAnimation);
	}
	
	public float GetAnimationDuration()
	{
		if (Animator?.SpriteFrames == null || _currentAnimation == null)
			return 0f;

		var frameCount = Animator.SpriteFrames.GetFrameCount(_currentAnimation);
		var animFps = Animator.SpriteFrames.GetAnimationSpeed(_currentAnimation);
		var speedScale = Animator.SpeedScale;

		// Prevent division by zero
		if (animFps <= 0 || speedScale <= 0)
			return float.PositiveInfinity;

		float totalDuration = 0f;

		// Sum the absolute duration of each frame to support custom frame timings
		for (int i = 0; i < frameCount; i++)
		{
			// GetFrameDuration returns relative duration (1.0 is default)
			float relativeDuration = Animator.SpriteFrames.GetFrameDuration(_currentAnimation, i);
        
			// Calculate absolute time for this specific frame
			var frameTime = (float)(relativeDuration / (animFps * speedScale));
			totalDuration += frameTime;
		}

		return totalDuration;
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
	public async Task WaitForFrame(int targetFrame)
	{
		while (Animator.IsPlaying() && _currentFrame != targetFrame)
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
	}

	/// <summary>
	/// Waits until the animation has looped a specific number of times.
	/// </summary>
	/// <param name="targetLoop">The target loop count to wait for.</param>
	public async Task WaitForLoopCount(int targetLoop)
	{
		while (Animator.IsPlaying() && _loopCount < targetLoop)
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
			if (_loopCount >= targetLoops && _currentFrame >= targetFrame)
				break;
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
	}

	/// <summary>
	/// Waits for a specific number of animation loops to complete.
	/// </summary>
	/// <param name="loopCount">The number of loops to wait for.</param>
	public async Task WaitForLoops(int loopCount)
	{
		var totalFrames = loopCount * GetAnimationFrameCount();
		await WaitForFrames(totalFrames);
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
