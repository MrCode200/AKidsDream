using AKidsDream.Core.Controllers;
using AKidsDream.Core.Managers;
using AKidsDream.Managers.SaveSystems;
using Godot;
using Godot.Collections;

namespace AKidsDream.UI;

public partial class EndTurnButton : TextureButton, IBlockable
{
	private PlayerId? _currentTurnPlayerId;
	public bool IsBlocked { get; set; }

	[Export] public Array<BlockingStrategy> Strategies { get; set; } =
		[BlockingStrategy.BlockOnBlockingTrigger, BlockingStrategy.BlockOnEffectApply];
	
	public override void _Ready()
	{
		EventBus.Instance.TurnStarted += OnTurnStarted;
		Disabled = true;
		BlockingManager.Instance.Register(this);
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
	
	public void SetBlocked(bool block)
	{
		if (block == IsBlocked) return;
		IsBlocked = block;
		Disabled = block;
	}
}
