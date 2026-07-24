using AKidsDream.GameBoard;
using Godot;

namespace AKidsDream.Units.FieldAccessPatterns;

public enum PatternMode
{
    Move,
    Attack,
}

[GlobalClass]
public abstract partial class AccessFieldPattern : Resource
{
    [Export] public PatternMode Mode { get; set; }
    public abstract Vector2I[] GetTiles(Vector2I origin, Board board, StatsData stats);
}