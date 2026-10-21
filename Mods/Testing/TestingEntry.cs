using AKidsDream.Commands;
using AKidsDream.Core.Content;
using AKidsDream.Core.Managers.ModManager;
using AKidsDream.Entities.Cards;
using AKidsDream.Util.Identifiers;
using Godot;

namespace AKidsDream.Mods.Core;

public class TestingEntry : IMod
{
    public void RegisterContent(IContentRegistrar registrar)
    {
        var unitPrefab = ResourceLoader.Load<PackedScene>("res://Mods/Testing/Units/Timmy/Timmy.tscn");
        registrar.RegisterUnit("timmy", new UnitDefinition(
            Id: new NamespacedKey(registrar.ModId, "timmy"),
            DisplayName: "Timmy",
            Description: "Testing unit",
            Icon: null,
            Prefab: unitPrefab
        ));

        var cardData = ResourceLoader.Load<AbilityCardData>("res://Mods/Testing/Units/Timmy/TimmyCardData.tres");
        if (cardData != null)
        {
            registrar.RegisterCard("spawn_timmy", cardData);
        }
    }

    public void Init(GameContext ctx)
    {
    }
}