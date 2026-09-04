using AKidsDream.GameBoard;
using Godot;

namespace AKidsDream.Abilities.Effects;

[GlobalClass]
[Tool]
public partial class AllTilesPattern : AccessFieldPattern
{
    public override Vector2I[] GetTilesUnfiltered(Vector2I? origin, Board board)
    {
        return board.GetAllTileLocations();
    }
}