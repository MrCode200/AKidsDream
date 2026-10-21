using Godot;
using System;
using System.Threading.Tasks;
using TweenComp.Resources.Blueprint;
using AKidsDream.Res.Common.Components.TweenComponent.Resources;
using TweenComp.Source;

namespace AKidsDream.Utilities.VFX;

[GlobalClass]
public partial class FloatingText : Label
{
    [Export] public TweenComponent TweenComponent;
    [Export] public Color DefaultColor = new(1, 1, 1);

    public override void _Ready()
    {
        SelfModulate = SelfModulate with { A = 0f };
    }

    public async void SetNewFloatingText(string text, Color? color = null, bool destroyOnFinish = false)
    {
        Text = text;
        if (color is { } clr)
            SelfModulate = SelfModulate with { R = clr.R, G = clr.G, B = clr.B };
        else if (SelfModulate with { A = 0 } != DefaultColor with { A = 0 })
            SelfModulate = SelfModulate with { R = DefaultColor.R, G = DefaultColor.G, B = DefaultColor.B };


        if (destroyOnFinish)
        {
            if (TweenComponent.PlayTween(TweenAnimationIdentifiers.FloatUpAndFade) != null)
            {
                await ToSignal(TweenComponent, TweenComponent.SignalName.FinishedPlayingTween);
                QueueFree();
            }
        }
        else
        {
            TweenComponent.PlayTween(TweenAnimationIdentifiers.FloatUpAndFade);
        }
    }
}