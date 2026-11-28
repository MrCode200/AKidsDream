using System.Linq;
using AKidsDream.GameBoard;
using Godot;

namespace AKidsDream.Abilities.Effects;

public partial class RectPattern : AccessFieldPattern
{
    [Export] public Vector2I Size { get; set; }
    // If the Rect Area is selected with origin as the center.
    // If false Origin is in the top left corner
    [Export] public bool FromCenter { get; set; }
    [Export] public bool RoundUp { get; set; }
    
    public override Vector2I[] GetTilesUnfiltered(Vector2I? origin, Board board)
    {
        if (origin == null)
            return [];

        var topLeft = origin.Value;
        if (FromCenter)
        {
            // 0, 1, 2, => 1 offset, with Round Up 3/2 = 1.5 rounded down;
            // 0, 1, 2, 3 => 1.5 round 1 is offset, with Round Up 2.5 = 2 thus rounded up;
            var offset = RoundUp ? (Size + Vector2I.One) / 2 : Size / 2;
            topLeft -= offset;
        }        
        
        return Enumerable.Range(topLeft.X, Size.X)
            .SelectMany(x => Enumerable.Range(topLeft.Y, Size.Y)
                .Select(y => new Vector2I(x, y)))
            .ToArray();
    }
}