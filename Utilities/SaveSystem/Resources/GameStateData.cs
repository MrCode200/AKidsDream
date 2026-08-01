using AKidsDream.Units.Resources;
using Godot;
using Godot.Collections;

namespace AKidsDream.Managers.SaveSystem.Resources;

[Tool]
public partial class GameStateData : Resource
{
    // TODO: GameRound
    [Export] public BoardStateData BoardStateData = new();
    [Export] public Array<UnitStateData> UnitStateResources = [];
}