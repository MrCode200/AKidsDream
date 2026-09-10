using System;
using Godot;

namespace AKidsDream.Utilities.TypeExtensions;

public static class Sprite2DExtensions
{
    public static void ScaleToMatch(this Sprite2D child, Sprite2D parent, float padding = 0f)
    {
        Vector2 childSize = child.Texture.GetSize();
        Vector2 availableSize = parent.Texture.GetSize();

        availableSize -= new Vector2(padding, padding);

        var fitScale = Math.Min(
            availableSize.X / childSize.X,
            availableSize.Y / childSize.Y
        );
        child.Scale = Vector2.One * fitScale;
    }
}