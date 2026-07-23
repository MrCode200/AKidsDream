using AKidsDream.Scripts;
using Godot;

namespace AKidsDream.Components;

/// <summary>
/// A reusable component that makes a Node2D selectable and hoverable.
/// Handles mouse interaction, selection state, hover state,
/// visual indicator toggling, and emits signals when states change.
/// </summary>
[GlobalClass]
public partial class SelectableComponent : Area2D
{
	// -- CONFIGURATION --
	[ExportGroup("Indicators")]
	/// <summary>
	/// Node that becomes visible when this object is selected.
	/// Usually a highlight sprite, outline, or selection circle.
	/// </summary>
	[Export]
	public Node2D SelectionIndicator;

	/// <summary>
	/// Node that becomes visible when the mouse is hovering over this object.
	/// </summary>
	[Export] public Node2D HoverIndicator;

	[ExportGroup("Mouse Control")]
	/// <summary>
	/// If enabled, left mouse clicks toggle the selection state.
	/// </summary>
	[Export(PropertyHint.InputName, "show_builtin")]
	public string SelectionAction;

	/// <summary>
	/// If enabled, mouse enter/exit events update the hover state.
	/// </summary>
	[Export] public bool OnMouseEnterHover = true;

	[ExportGroup("Other")]
	/// <summary>
	/// Name of the event bus signal to emit when the selection state changes.
	/// </summary>
	[Export] public StringName OnSelectCallEventBus;
	/// <summary>
	/// Name of the event bus signal to emit when the selection state changes.
	/// </summary>
	[Export] public StringName OnDeselectCallEventBus;

	// -- STATE --

	private bool _isSelected;
	private bool _isHovered;

	/// <summary>
	/// Whether this object is currently selected.
	/// Changing this also updates the selection indicator and emits <see cref="Selected"/>.
	/// </summary>
	public bool IsSelected
	{
		get => _isSelected;
		set
		{
			if (_isSelected == value)
				return;

			_isSelected = value;

			if (SelectionIndicator is not null)
				SelectionIndicator.Visible = _isSelected;

			EmitSignal(SignalName.Selected, _isSelected);
			if (_isSelected && !string.IsNullOrEmpty(OnSelectCallEventBus))
				EventBus.Instance.EmitSignal(OnSelectCallEventBus, GetParent());
			if (!_isSelected && !string.IsNullOrEmpty(OnDeselectCallEventBus))
				EventBus.Instance.EmitSignal(OnDeselectCallEventBus, GetParent());
		}
	}


	/// <summary>
	/// Whether the mouse is currently hovering over this object.
	/// Changing this also updates the hover indicator and emits <see cref="Hovered"/>.
	/// </summary>
	public bool IsHovered
	{
		get => _isHovered;
		set
		{
			if (_isHovered == value)
				return;

			_isHovered = value;

			if (HoverIndicator is not null)
				HoverIndicator.Visible = _isHovered;

			EmitSignal(SignalName.Hovered, _isHovered);
		}
	}


	// -- SIGNALS --

	/// <summary>
	/// Emitted when the selection state changes.
	/// </summary>
	[Signal] public delegate void SelectedEventHandler(bool selected);


	/// <summary>
	/// Emitted when the hover state changes.
	/// </summary>
	[Signal] public delegate void HoveredEventHandler(bool hovered);


	// -- LIFECYCLE --

	public override void _Ready()
	{
		SetupInputHandlers();
		UpdateIndicators();
	}


	// -- LOGIC --

	/// <summary>
	/// Connects enabled mouse interaction handlers.
	/// </summary>
	private void SetupInputHandlers()
	{
		if (!string.IsNullOrEmpty(SelectionAction))
			InputEvent += OnInputEvent;

		if (OnMouseEnterHover)
		{
			MouseEntered += OnMouseEntered;
			MouseExited += OnMouseExited;
		}
	}


	/// <summary>
	/// Synchronizes indicator visibility with the current state.
	/// Useful after loading or initializing the component.
	/// </summary>
	private void UpdateIndicators()
	{
		if (SelectionIndicator is not null) SelectionIndicator.Visible = _isSelected;
		if (HoverIndicator is not null) HoverIndicator.Visible = _isHovered;
	}


	private void OnMouseEntered() => IsHovered = true;
	private void OnMouseExited() => IsHovered = false;

	/// <summary>
	/// Handles mouse clicks on this Area2D.
	/// Toggles selection when the Action is pressed.
	/// </summary>
	private void OnInputEvent(
		Node viewport,
		InputEvent inputEvent,
		long shapeIdx
	)
	{
		if (!string.IsNullOrEmpty(SelectionAction) &&
			inputEvent.IsActionPressed(SelectionAction))
		{
			IsSelected = !IsSelected;
		}
	}
}
