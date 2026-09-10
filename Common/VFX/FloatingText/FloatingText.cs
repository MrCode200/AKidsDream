using Godot;
using System;
using AKidsDream.Common.Components.TweenComponent.Resources;
using AKidsDream.Res.Common.Components.TweenComponent.Resources;

namespace AKidsDream.Common.VFX;

[GlobalClass]
public partial class FloatingText : Label
{
	[Export] public TweenComponent TweenComponent;
	[Export] public Color DefaultColor = new (1, 1, 1);

	public override void _Ready()
	{
		SelfModulate = SelfModulate with { A = 0f };
	}

	public void SetNewFloatingText(string text, Color? color = null)
	{
		Text = text;
		if (color is { } clr)
			SelfModulate = SelfModulate with { R = clr.R, G = clr.G, B = clr.B };
		else if (SelfModulate with { A = 0 } != DefaultColor with { A = 0 })
			SelfModulate = SelfModulate with { R = DefaultColor.R, G = DefaultColor.G, B = DefaultColor.B };

		TweenComponent.PlayTween(TweenAnimationIdentifiers.FloatUpAndFade);
	}
}
