using Godot;
using System;
using AKidsDream.GameBoard;
using AKidsDream.Managers.SaveSystems;

public partial class InputMoveComponent : Camera2D
{
	[Export] public Board Board;
	// --- Exported Variables (Adjust in Inspector) ---
	
	[ExportCategory("Zoom Settings")]
	[Export] public float MinZoom = 0.5f;
	[Export] public float MaxZoom = 3.0f;
	[Export] public float ZoomSpeed = 0.1f;
	[Export] public float ZoomSmoothing = 5.0f; // Higher = faster snap

	[ExportCategory("Pan Settings")]
	[Export] public float PanSpeed = 400f; // Pixels per second
	[Export] public float PanSmoothing = 1.0f; // Higher = faster snap

	[ExportCategory("Input Actions")]
	// Default Godot actions or custom ones you create in Project Settings > Input Map
	[Export(PropertyHint.InputName, "show_builtin")] public string ActionPanUp;
	[Export(PropertyHint.InputName, "show_builtin")] public string ActionPanDown;
	[Export(PropertyHint.InputName, "show_builtin")] public string ActionPanLeft;
	[Export(PropertyHint.InputName, "show_builtin")] public string ActionPanRight;
	[Export(PropertyHint.InputName, "show_builtin")] public string ActionZoomIn; 
	[Export(PropertyHint.InputName, "show_builtin")] public string ActionZoomOut;

	[ExportCategory("Mouse Fallbacks")]
	//[Export] public bool EnableMouseWheelZoom { get; set; } = true;
	//[Export] public MouseButton PanMouseButton { get; set; } = MouseButton.Middle;

	// --- Internal State ---
	private Vector2 _targetZoom;
	private Vector2 _targetPosition;
	private Vector2 _mousePositionBeforeZoom;

	public override async void _Ready()
	{
		// Center position to board upon creation
		await ToSignal(EventBus.Instance, EventBus.SignalName.GameInitialized);
		Position += new Vector2(Board.StateData.Width * Global.TileSize / 2,
			Board.StateData.Height * Global.TileSize / 2) - GetScreenCenterPosition();
		
		_targetZoom = Zoom;
		_targetPosition = Position;
	}

	public override void _Process(double delta)
	{
		HandleZoomInput();
		HandlePanInput((float)delta);

		// Apply Smoothing (Lerp)
		// We use Interpolate to create smooth motion towards the target values
		Zoom = Zoom.Lerp(_targetZoom, (float)(ZoomSmoothing * delta));
		Position = Position.Lerp(_targetPosition, (float)(PanSmoothing));
	}

	private void HandleZoomInput()
	{
		if (!string.IsNullOrEmpty(ActionZoomIn) && Input.IsActionJustReleased(ActionZoomIn))
		{
			PerformZoom(1.0f + ZoomSpeed);
		}
		
		if (!string.IsNullOrEmpty(ActionZoomOut) && Input.IsActionJustReleased(ActionZoomOut))
		{
			PerformZoom(1.0f - ZoomSpeed);
		}
	}

	private void PerformZoom(float factor)
	{
		// Apply zoom factor
		_targetZoom *= factor;

		// Clamp zoom limits
		_targetZoom = new Vector2(
			Math.Clamp(_targetZoom.X, MinZoom, MaxZoom),
			Math.Clamp(_targetZoom.Y, MinZoom, MaxZoom)
		);
	}

	private void HandlePanInput(float delta)
	{
		var inputDirection = Input.GetVector(ActionPanLeft, ActionPanRight, ActionPanUp, ActionPanDown);

		// Normalize to prevent faster diagonal movement
		if (inputDirection.Length() > 1)
			inputDirection = inputDirection.Normalized();

		// Move target position based on direction and speed
		// We multiply by delta to make it frame-rate independent
		// We divide by Zoom so panning feels consistent regardless of zoom level
		_targetPosition += inputDirection * PanSpeed * delta / Zoom.X;
	}
}
