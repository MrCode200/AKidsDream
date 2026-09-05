using Godot;
using System;
using AKidsDream.Common.Components.TweenComponent.Resources;
using AKidsDream.Res.Common.Components.TweenComponent.Resources;

namespace AKidsDream.Common.VFX;

[GlobalClass]
public partial class FloatingText : Label
{
	[Export] public TweenComponent TweenComponent;
	[Export] public Color DefaultColor = new Color(1, 1, 1);

	public override void _Ready()
	{
		Modulate = Modulate with { A = 0f };
	}

	public void SetNewFloatingText(string text, Color? color = null)
	{
		Text = text;
		if (color is { } clr)
			Modulate = Modulate with { R = clr.R, G = clr.G, B = clr.B };
		else if (Modulate != DefaultColor)
			Modulate = Modulate with { R = DefaultColor.R, G = DefaultColor.G, B = DefaultColor.B };
		
		TweenComponent.PlayTween(TweenAnimationIdentifiers.FloatUpAndFade);
	}
}
