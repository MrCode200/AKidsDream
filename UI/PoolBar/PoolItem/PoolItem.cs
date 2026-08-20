#nullable enable
using Godot;
using System;
using AKidsDream.Units.Resources;

public partial class PoolItem : Control
{
	[ExportCategory("UI Nodes")]
	[Export] public HBoxContainer PoolContainer = null!;
	[Export] public Label PoolLabel = null!;
	[Export] public Label DeltaCostLabel = null!;
	[Export] public Sprite2D PoolIcon = null!;
	
	[ExportCategory("Animation Parameters")]
	[Export] public float SecondsPerValue = 0.2f;
	[Export] public float MinTweenDuration = 0.3f;
	[Export] public float MaxTweenDuration = 2;

	private Tween? _activeTween;
	private PoolData? _currentPool;
	private int _displayedCount;

	/// <summary>
	/// It will update the pool item if the pool is different from the current pool,
	/// or animate the count change if the count is different. 
	/// </summary>
	/// <param name="pool"></param>
	public void SetPoolItem(PoolData pool)
	{
		if (pool.Name != _currentPool?.Name)
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
			).SetTrans(Tween.TransitionType.Quint)
			.SetEase(Tween.EaseType.InOut);
	}
	
	private void UpdateLabelValue(int value)
	{
		PoolLabel.Text = $"{value} / {_currentPool?.MaxCount}";
	}
	
	public void UpdatePoolPreview(PoolData pool, int previewCost)
	{
		if (_currentPool?.Name != pool.Name) return;
		
		var costChange = previewCost switch
		{
			< 0 => $"[color=#E74C3C]({previewCost})[/color]",  // Red: (-3)
			> 0 => $"[color=#4CD964](+{previewCost})[/color]", // Green: (+3)
			0 => $"[color=#A0A0A0](=0)[/color]",               // Grey: (=0)
		};
		DeltaCostLabel.Text = costChange;
	}
}
