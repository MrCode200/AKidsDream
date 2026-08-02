using AKidsDream.Managers.SaveSystems;
using Godot;

namespace AKidsDream.UI;

public partial class EndTurnButton : TextureButton
{
	public override void _Ready()
	{
		EventBus.Instance.NewTurnStarted += (int _) => Disabled = false;
	}

	public override void _Pressed()
	{
		EventBus.Instance.EmitSignal(EventBus.SignalName.EndTurnBtnPressed);
		Disabled = true;
	}
}
