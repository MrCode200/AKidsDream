using AKidsDream.Managers.SaveSystems;
using Godot;

namespace AKidsDream.UI;

public partial class EndTurnButton : TextureButton
{
	private void OnLocalPlayerTurnEnded(int _, int __) => Disabled = false;

	public override void _Ready()
	{
		EventBus.Instance.LocalPlayerTurnStarted += OnLocalPlayerTurnEnded;
	}

	public override void _Pressed()
	{
		// TODO: make end turn command...
		
		Disabled = true;
	}

	public override void _ExitTree()
	{
		EventBus.Instance.LocalPlayerTurnStarted -= OnLocalPlayerTurnEnded;
	}
}
