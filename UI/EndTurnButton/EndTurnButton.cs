using AKidsDream.Core.Controllers;
using AKidsDream.Core.Managers;
using AKidsDream.Managers.SaveSystems;
using Godot;

namespace AKidsDream.UI;

public partial class EndTurnButton : TextureButton
{
	private PlayerId? _currentTurnPlayerId;

	public override void _Ready()
	{
		EventBus.Instance.TurnStarted += OnTurnStarted;
		Disabled = true;
	}
	
	public override void _ExitTree()
	{
		EventBus.Instance.TurnStarted -= OnTurnStarted;
	}

	// -- LOGIC --

	private void OnTurnStarted(int playerIdInt, int round)
	{
		_currentTurnPlayerId = new PlayerId(playerIdInt);
		Disabled = !HasPlayerInteractionController(playerIdInt);
	}

	public override void _Pressed()
	{
		if (_currentTurnPlayerId is null) return;
		Disabled = true;
		EventBus.Instance.EmitSignal(EventBus.SignalName.EndTurnButtonPressed, _currentTurnPlayerId.Value.Value);
	}


	private static bool HasPlayerInteractionController(int playerIdInt)
	{
		GameManager.Instance.PlayerTeamRegistry.TryGetPlayer(new PlayerId(playerIdInt), out var playerData);
		return playerData?.ControllerType == ControllerType.PlayerInteractionController;
	}
}
