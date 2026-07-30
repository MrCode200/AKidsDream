using AKidsDream.Units;
using Godot;
using Godot.Collections;

namespace AKidsDream.resources.stateResources;

[Tool]
public partial class GameStateData : Resource
{
    [Export] public BoardState BoardState = new();
    [Export] public Array<UnitStateData> UnitStateResources = [];
}