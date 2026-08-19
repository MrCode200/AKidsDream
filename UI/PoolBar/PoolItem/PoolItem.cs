#nullable enable
using Godot;
using System;
using AKidsDream.Units.Resources;

// TODO: make instead of PoolId, uniquePoolName? (as Name is identifier for Id, and has to be unique (?)
public partial class PoolItem : Control
{
	[ExportCategory("UI Nodes")]
	[Export] public HBoxContainer PoolContainer = null!;
	[Export] public Label PoolLabel = null!;
	[Export] public Sprite2D PoolIcon = null!;
	
	[ExportCategory("Animation Parameters")]
	[Export] public float SecondsPerValue = 0.2f;
	[Export] public float MinTweenDuration = 0.3f;
	[Export] public float MaxTweenDuration = 2;

	private Tween? _activeTween;
	private PoolData? _currentPool;
	private int _displayedCount;
	
	public void SetPoolItem(PoolData pool)
	{
		if (pool.PoolId != _currentPool?.PoolId)
		{
			_currentPool = pool;
			PoolIcon.Texture = pool.Icon;
			PoolContainer.TooltipText = pool.Name;

			UpdateLabelValue(pool.CurrentCount);
			_displayedCount = pool.CurrentCount;
		}
		else if (_displayedCount != pool.CurrentCount)
		{
			AnimateCountChange(_displayedCount, pool.CurrentCount);
			_displayedCount = pool.CurrentCount;
		}
	}
	
	public PoolId GetPoolId()
	{
		return _currentPool?.PoolId ?? PoolId.None;
	}
	
	private void AnimateCountChange(int startValue, int endValue)
	{
		_activeTween?.Kill();
		
		int change = Mathf.Abs(endValue - startValue);
		float duration = Math.Clamp(
			change * SecondsPerValue,
			MinTweenDuration,
			MaxTweenDuration
		);

		_activeTween = CreateTween();
		_activeTween.TweenMethod(Callable.From<int>(UpdateLabelValue),
			startValue,
			endValue,
			duration
			).SetTrans(Tween.TransitionType.Cubic)
			.SetEase(Tween.EaseType.InOut);
	}
	
	private void UpdateLabelValue(int value)
	{
		PoolLabel.Text = $"{value} / {_currentPool?.MaxCount}";
	}
	
	// CHECK: try to make above method only for creation, and below for updating already set pool
	public void UpdatePoolPreview(PoolData pool, int previewCost)
	{
		var costChange = previewCost switch
		{
			< 0 => $"[color=#E74C3C]({previewCost})[/color]",  // Red: (-3)
			> 0 => $"[color=#4CD964](+{previewCost})[/color]", // Green: (+3)
			0 => $"[color=#A0A0A0](=0)[/color]",               // Grey: (=0)
		};
		PoolLabel.Text = $"{pool.CurrentCount} / {pool.MaxCount} {costChange}";
	}
}
