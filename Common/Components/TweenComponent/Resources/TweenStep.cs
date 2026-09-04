#nullable enable
using Godot;
using Godot.Collections;

namespace AKidsDream.Common.Components.TweenComponent.Resources;

public enum TreeOrderAction
{
    None,
    MoveToFront,
    MoveToBack,
    MoveToIndex
}

[GlobalClass]
[Tool]
public partial class TweenStep : Resource
{
    [ExportGroup("Property")]
    private NodePath _property = new();
    [Export]
    public NodePath Property
    {
        get => _property;
        set
        {
            _property = value;
            NotifyPropertyListChanged();
        }
    }
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
    
    [ExportGroup("Other")]
    private TreeOrderAction _treeOrderAction = TreeOrderAction.None;

    [Export]
    public TreeOrderAction TreeOrderAction
    {
        get => _treeOrderAction;
        set
        {
            _treeOrderAction = value;
            NotifyPropertyListChanged();
        }
    }
    public int FromTreeIndex;
    [Export] public int ToTreeIndex;
    
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

    [ExportGroup("")] 
    private bool _disable;

    [Export]
    public bool Disable
    {
        get => _disable;
        set
        {
            _disable = value;
            NotifyPropertyListChanged();
        }
    }


    public override void _ValidateProperty(Dictionary property)
    {
        var propertyName = property["name"].AsString();

        bool show = propertyName switch
        {
            nameof(FromValue) => UseExplicitFrom,
            nameof(Delay) => !RunParallelWithPrevious,
            nameof(ToTreeIndex) => TreeOrderAction == TreeOrderAction.MoveToIndex,
            _ => true
        };
        
        if (!show)
        {
            property["usage"] = (int)PropertyUsageFlags.NoEditor;
            return;
        }

        bool disable = propertyName switch
        {
            nameof(UseExplicitFrom) 
                or nameof(FromValue)
                or nameof(ToValue)
                or nameof(SetValueRelative)
                or nameof(Duration)
                or nameof(Transition)
                or nameof(Ease) => Property.IsEmpty,
            
            nameof(Disable) => false,
            
            _ => Disable
        };
        
        if (disable)
            property["usage"] = (int)(property["usage"].AsInt32() | (long)PropertyUsageFlags.ReadOnly);
    }
}