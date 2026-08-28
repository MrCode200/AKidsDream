#nullable enable
using Godot;
using Godot.Collections;

namespace AKidsDream.Common.Components.TweenComponent.Resources;

[GlobalClass]
[Tool]
public partial class TweenStep : Resource
{
    [ExportGroup("Property")]
    [Export] public NodePath Property = new();
    private bool _useExplicitFrom;
    [Export] public bool UseExplicitFrom
    {
        get => _useExplicitFrom;
        set
        {
            _useExplicitFrom = value;
            NotifyPropertyListChanged();
        }
    }
    [Export] public Variant FromValue;
    [Export] public Variant ToValue;
    [Export] public bool SetValueRelative;
    
    [ExportGroup("Timing")]
    [Export(PropertyHint.Range, "0.001,10,,or_greater")] public float Duration = 0.3f;
    [Export(PropertyHint.Range, "0,10,,or_greater")] public float Delay;
    [Export] public Tween.TransitionType Transition = Tween.TransitionType.Linear;
    [Export] public Tween.EaseType Ease = Tween.EaseType.InOut;
    
    [ExportCategory("Other")]
    private bool _runParallelWithPrevious;
    [Export] public bool RunParallelWithPrevious
    {
        get => _runParallelWithPrevious;
        set
        {
            _runParallelWithPrevious = value;
            NotifyPropertyListChanged();
        }
    }
    [Export] public NodePath TargetOverride = "";
    [Export] public bool Disable;


    public override void _ValidateProperty(Dictionary property)
    {
        var propertyName = property["name"].AsString();

        bool show = propertyName switch
        {
            nameof(FromValue) => UseExplicitFrom,
            nameof(Delay) => !RunParallelWithPrevious,
            _ => true
        };

        if (!show)
            property["usage"] = (int)PropertyUsageFlags.NoEditor;
    }
}