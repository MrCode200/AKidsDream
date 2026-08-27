#nullable enable
using Godot;
using System;
using AKidsDream.Common;

// TODO: add blinking red for CurrentValue/MaxValue if PreviewCount > CurrentCount

public partial class PoolItem : Control
{
    [ExportCategory("UI Nodes")] [Export] public HBoxContainer PoolContainer = null!;
    [Export] public Label PoolLabel = null!;
    [Export] public Label DeltaCostLabel = null!;
    [Export] public Sprite2D PoolIcon = null!;

    [ExportCategory("Animation Parameters")] [Export]
    public float SecondsPerValue = 0.2f;

    [Export] public float MinTweenDuration = 0.3f;
    [Export] public float MaxTweenDuration = 2;

    private Tween? _countTween;
    private Tween? _previewTween;
    private PoolData? _currentPool;
    private int _displayedCount;
    private Vector2 _originalPreviewLabelScale;

    public override void _Ready()
    {
        DeltaCostLabel.Text = "";
        _originalPreviewLabelScale = DeltaCostLabel.Scale;
    }

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

            UpdateLabelCurrentValue(pool.CurrentCount);
            ResetPoolPreview();
            _displayedCount = pool.CurrentCount;
        }
        else if (_displayedCount != pool.CurrentCount)
        {
            _countTween?.Kill();
            _countTween = AnimateCountChange(_displayedCount, pool.CurrentCount, UpdateLabelCurrentValue);
            _displayedCount = pool.CurrentCount;
        }
    }

    private Tween AnimateCountChange(int startValue, int endValue, Action<int> updateLabel)
    {
        int change = Mathf.Abs(endValue - startValue);
        float duration = Math.Clamp(
            change * SecondsPerValue,
            MinTweenDuration,
            MaxTweenDuration
        );

        var tween = CreateTween();
        tween.TweenMethod(Callable.From(updateLabel),
                startValue,
                endValue,
                duration
            ).SetTrans(Tween.TransitionType.Quint)
            .SetEase(Tween.EaseType.InOut);


        return tween;
    }

    private void UpdateLabelCurrentValue(int value)
    {
        PoolLabel.Text = $"{value} / {_currentPool?.MaxCount}";
    }

    public void UpdatePoolPreview(int previewCost, bool reverseSign = true)
    {
        if (reverseSign)
            previewCost = -previewCost;

        DeltaCostLabel.Text = previewCost switch
        {
            < 0 => $"({previewCost})",
            > 0 => $"(+{previewCost})",
            _ => "(=0)",
        };

        var color = previewCost switch
        {
            < 0 => new Color("#E74C3C"),
            > 0 => new Color("#4CD964"),
            _ => new Color("#A0A0A0"),
        };

        DeltaCostLabel.LabelSettings.FontColor = color;
    }

    public async void ResetPoolPreview()
    {
        var currentValue = 0;
        if (!string.IsNullOrEmpty(DeltaCostLabel.Text))
        {
            var inner = DeltaCostLabel.Text.Replace("(", "").Replace(")", "");
            if (!inner.StartsWith("="))
                currentValue = int.Parse(inner);
        }

        // No need to animate if the value is already 0
        if (currentValue == 0)
        {
            DeltaCostLabel.Text = "";
            return;
        }

        var endvalue = (currentValue > 0) ? 1 : -1;

        _previewTween?.Kill();
        _previewTween = AnimateCountChange(currentValue, endvalue, (value) => UpdatePoolPreview(value, false));
        await ToSignal(_previewTween, Tween.SignalName.Finished);
        DeltaCostLabel.Text = "";
    }
}