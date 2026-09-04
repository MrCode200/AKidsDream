using AKidsDream.Abilities;
using AKidsDream.Abilities.Effects;
using AKidsDream.Common;
using AKidsDream.Common.Components.TweenComponent.Resources;
using AKidsDream.Entities.Cards;
using Godot;

namespace AKidsDream.Managers.SaveSystems;

public partial class EventBus : Node
{
    public static EventBus Instance { get; private set; }
    
    [Signal] public delegate void CallDeferredReachedEventHandler();

    public override void _Process(double delta)
    {
        CallDeferred(nameof(EmitCallDeferredReached));
    }
    
    private void EmitCallDeferredReached()
    {
        EmitSignal(SignalName.CallDeferredReached);
    }

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _ExitTree()
    {
        Instance = null;
    }

    // -- GAME SIGNALS --

    [Signal]
    public delegate void GameInitializedEventHandler();

    [Signal]
    public delegate void NewRoundStartedEventHandler(int playerIdInt, int newRound);

    [Signal]
    public delegate void TurnStartedEventHandler(int playerIdInt, int round);

    [Signal]
    public delegate void TurnEndedEventHandler(int playerIdInt, int round);

    // [Signal] public delegate void ActivePlayerChangedEventHandler(int previousPlayerIdInt, int currentPlayerIdInt, int round);

    [Signal]
    public delegate void RoundStartedEventHandler(int round);

    // -- UNIT SIGNALS --

    // NOTE: Unit Created gets emitted on _Ready not on Unit.Init(...);

    [Signal]
    public delegate void UnitCreatedEventHandler(Unit unit);

    [Signal]
    public delegate void UnitSelectedEventHandler(Unit unit);

    [Signal]
    public delegate void UnitChangedEventHandler(Unit oldUnit, Unit newUnit);
    
    [Signal]
    public delegate void UnitDeselectedEventHandler(Unit unit);

    [Signal]
    public delegate void UnitKilledEventHandler(Unit unit);

    [Signal]
    public delegate void UnitMovedEventHandler(Unit unit, Vector2I oldTile, Vector2I newTile);

    // -- PIC --
    [Signal]
    public delegate void NewTileHoveredEventHandler(Unit unit, AbilityContext ctx, AbilityPayload payload);

    // -- Abilities --

    [Signal]
    public delegate void AbilityCastStartEventHandler(Unit unit, AbilityData abilityData);

    [Signal]
    public delegate void AbilityCastEndEventHandler(Unit unit, AbilityData ability, EffectResult result);

    [Signal]
    public delegate void AbilityCostUpdatedEventHandler(Unit unit, AbilityData ability, int newCount);

    [Signal]
    public delegate void EffectTriggerStartEventHandler(Node caster, AbilityData ability, EffectData effect);

    [Signal]
    public delegate void EffectTriggerEndEventHandler(Node caster, AbilityData ability, EffectData effect);

    [Signal]
    public delegate void EffectApplyStartEventHandler(Node caster, AbilityData ability, EffectData effect);

    [Signal]
    public delegate void EffectApplyEndEventHandler(Node caster, AbilityData ability, EffectData effect,
        EffectResult result);

    [Signal]
    public delegate void AbilityDeselectedEventHandler(Unit unit);
    
    // -- CARD SIGNALS --
    [Signal] public delegate void CardSelectedEventHandler(AbilityCard card);
    [Signal] public delegate void CardChangedEventHandler(AbilityCard oldCard, AbilityCard newCard);
    [Signal] public delegate void CardDeselectedEventHandler(AbilityCard card);

    // -- UI SIGNALS --

    [Signal]
    public delegate void AbilityBtnPressedEventHandler(Unit unit, AbilityData ability);

    [Signal]
    public delegate void EndTurnButtonPressedEventHandler(int callerPlayerIdInt);
}