using AKidsDream.Core.Managers;
using AKidsDream.Core.Teams;
using AKidsDream.Managers.SaveSystems;
using Godot;
using Godot.Collections;

namespace AKidsDream.UI;

public partial class EndTurnButton : TextureButton, IBlockable
{
	public bool IsBlocked { get; set; }

	[Export] public Array<BlockingStrategy> BlockingStrategies { get; set; } =
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

	private void OnTurnStarted(PlayerData player, int round)
	{
		Disabled = !player.Controller.UseEndTurnButton;
	}

	public override void _Pressed()
	{
		Disabled = true;
		EventBus.Instance.EmitSignal(EventBus.SignalName.EndTurnButtonPressed);
	}
	
	public void SetBlocked(bool block)
	{
		if (block == IsBlocked) return;
		IsBlocked = block;
		Disabled = block;
	}
}
