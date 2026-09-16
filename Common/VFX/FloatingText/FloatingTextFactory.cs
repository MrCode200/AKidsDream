using Godot;

namespace AKidsDream.Common.VFX;

[GlobalClass]  
public partial class FloatingTextFactory : Control
{
    [Export] public PackedScene FloatingTextScene;
    public Color DefaultColor { get; set; }

    public void SpawnFloatingText(string text, Color? color = null)
    {
        var floatingText = FloatingTextScene.Instantiate<FloatingText>();
        AddChild(floatingText);
        floatingText.SetNewFloatingText(text, color ?? DefaultColor, true);
        floatingText.Position = Position;
    }
}