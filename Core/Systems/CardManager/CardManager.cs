using System;
using System.Threading.Tasks;
using AKidsDream.Abilities.Effects;
using AKidsDream.Commands;
using AKidsDream.Common.Components.TweenComponent.Resources;
using AKidsDream.Common.Logging;
using AKidsDream.Entities.Cards;
using AKidsDream.GameBoard;
using AKidsDream.Managers;
using AKidsDream.Managers.SaveSystems;
using Godot;
using Serilog;

namespace AKidsDream.Core.Managers;

/*
 * Card Selection rules:
 * 1. When click and no card selected, select
 * 2. When click on the ALREADY SELECTED card deselect
 * 3. When click on NEW CARD, switch selection
 * 4. When click and dragging, drag to position, when no more pressing, stop dragging, but don't deselect
 * 4.1 While Dragging: get tile under card
 * 4.2 on letting go, cast card on that tile
 */

[Icon("res://Core/Systems/CardManager/playing-cards.png")]
public partial class CardManager : Node2D
{
    [Export(PropertyHint.InputName, "show_builtin")]
    public string SelectionInputName;

    [Export] public float DragThreshold = 10;

    [ExportGroup("Dependencies")] [Export] public AbilityVisualizer AbilityVisualizer;
    [Export] public PlayerHand PlayerHand;

    public AbilityCard SelectedCard;

    private bool _isDragging;
    private bool _isPressed;

    private Vector2? _cardDraggingAnchor;
    private Vector2 _pressPosition;
    private Vector2 _screenSize;
    private GameContext _gameContext;

    private AbilityContext _cachedAbilityContext;

    // Doesn't change as there are no ability.state mutating operations
    private AbilityPayload _cachedAbilityPayload;

    private readonly ILogger _log = Log.ForContext<CardManager>();

    public override void _Ready()
    {
        _screenSize = GetViewport().GetVisibleRect().Size;
        _cachedAbilityPayload = new AbilityPayload
        {
            CurrentOrigin = new Vector2I(-1, -1),
            State = new AbilityState()
        };
    }

    public void Init(GameContext gameContext)
    {
        _gameContext = gameContext;
    }

    public override void _Input(InputEvent @event)
    {
        if (string.IsNullOrEmpty(SelectionInputName)) return;

        // Check if Card hovered
        var hoveredControl = GetViewport().GuiGetHoveredControl();
        if (hoveredControl is not AbilityCard clickedCard) return;

        // Handle PRESS: save state
        if (@event.IsActionPressed(SelectionInputName))
        {
            _pressPosition = GetViewport().GetMousePosition();
            _isDragging = false;
            _isPressed = true;
            _cardDraggingAnchor = null;
        }
        // Handle RELEASE: check if click or drag end
        else if (@event.IsActionReleased(SelectionInputName))
        {
            _isPressed = false;

            if (_isDragging)
            {
                // Rule 4: Stop dragging, but don't deselect
                TryCastCard();
                _isDragging = false;
                ClearAbilityContextPayload();
            }
            else
            {
                // It's a click - handle selection rule
                HandleCardClick(clickedCard);
            }
        }
        // Rule 4: Handle HOLD: check if drag threshold reached
        else if (_isPressed && @event is InputEventMouseMotion mouseMotion)
        {
            float distance = _pressPosition.DistanceTo(mouseMotion.Position);
            if (distance >= DragThreshold && !_isDragging)
            {
                _isDragging = true;
                _cardDraggingAnchor = mouseMotion.Position - clickedCard.Position;

                // Select when started dragging, while mouse is pressed
                if (SelectedCard is null || SelectedCard != clickedCard)
                    HandleCardClick(clickedCard);

                // Initialize cached context and payload when dragging starts
                BuildAbilityContextPayload();
            }
        }
    }

    public override void _Process(double delta)
    {
        if (SelectedCard is null || !_isDragging || _cardDraggingAnchor is null) return;

        UpdateCardPosition(); // TODO: lerp card towards mouse pos
        GetMouseTile();
    }

    // -- LOGIC --
    private void HandleCardClick(AbilityCard clickedCard)
    {
        // 1. When click and no card is selected, select
        if (SelectedCard is null)
        {
            SelectCard(clickedCard);
        }
        // 2. When click on the ALREADY SELECTED card deselect
        else if (SelectedCard.Id == clickedCard.Id)
        {
            DeselectCard(clickedCard);
        }
        // 3. When click on NEW CARD, switch selection
        else if (SelectedCard.Id != clickedCard.Id)
        {
            ChangeCard(clickedCard);
        }
    }

    private void DeselectCard(AbilityCard clickedCard)
    {
        _log.Here().Debug("Deselected card: {NameTag}:{IdTag}", SelectedCard.CardData.Name, SelectedCard.Id);

        SelectedCard.IsSelected = false;
        SelectedCard = null;
        _cardDraggingAnchor = null;
        ClearAbilityContextPayload();
        _gameContext.AbilityVisualizer.ClearTilemaps();

        EventBus.Instance.EmitSignal(EventBus.SignalName.CardDeselected, clickedCard);
    }

    private void SelectCard(AbilityCard clickedCard)
    {
        SelectedCard = clickedCard;
        BuildAbilityContextPayload();

        _gameContext.AbilityVisualizer.ShowReachVisualization(
            _cachedAbilityContext,
            _cachedAbilityPayload,
            SelectedCard.CardData.Ability
        );

        _log.Here().Debug("Selected card: {NameTag}:{IdTag}", SelectedCard.CardData.Name, SelectedCard.Id);
        EventBus.Instance.EmitSignal(EventBus.SignalName.CardSelected, clickedCard);
    }

    private void ChangeCard(AbilityCard clickedCard)
    {
        var oldCard = SelectedCard;
        oldCard.IsSelected = false;

        SelectedCard = clickedCard;
        SelectedCard.IsSelected = true;

        BuildAbilityContextPayload();
        _gameContext.AbilityVisualizer.ShowReachVisualization(
            _cachedAbilityContext,
            _cachedAbilityPayload,
            SelectedCard.CardData.Ability
        );

        _log.Here().Debug("Changed Card {OldCardName}:{OldIdTag} to {NewCardName}:{NewIdTag}",
            oldCard.CardData.Name, oldCard.Id, SelectedCard.CardData.Name, SelectedCard.Id);
        EventBus.Instance.EmitSignal(EventBus.SignalName.CardChanged, oldCard, SelectedCard);
    }

    // -- WHILE SELECTED --
    private void UpdateCardPosition()
    {
        var mousePos = GetViewport().GetMousePosition();
        var newCardPos = mousePos - _cardDraggingAnchor!.Value;

        SelectedCard.Position = new Vector2(
            Mathf.Clamp(newCardPos.X, 0, _screenSize.X - SelectedCard.Size.X),
            Mathf.Clamp(newCardPos.Y, 0, _screenSize.Y - SelectedCard.Size.Y)
        );
    }

    private void GetMouseTile()
    {
        var mousePos = GetGlobalMousePosition();
        var mouseTile = Board.WorldPositionToTilePosition(mousePos);

        // Update cached payload with new tile position
        _cachedAbilityPayload.AccumulatedTargets = [mouseTile];
        _cachedAbilityPayload.ProcessingTiles = [mouseTile];

        _gameContext.AbilityVisualizer.ShowEffectVisualization(
            _cachedAbilityContext,
            _cachedAbilityPayload,
            SelectedCard.CardData.Ability.Effects
        );
    }

    // -- ABILITY CASTING --
    private void BuildAbilityContextPayload()
    {
        if (SelectedCard is null) return;

        var cardCaster = new CardCaster(
            SelectedCard.Id,
            _gameContext.GameLoopManager.ActivePlayerId,
            $"{SelectedCard.CardData.Name}Card"
        );

        _cachedAbilityContext = new AbilityContext
        {
            Caster = cardCaster, // Will be updated in GetMouseTile
            CasterNode = SelectedCard,
            Ability = SelectedCard.CardData.Ability,
            GameContext = _gameContext
        };
    }

    private void ClearAbilityContextPayload()
    {
        _cachedAbilityContext = null;
        _cachedAbilityPayload.AccumulatedTargets = [];
        _cachedAbilityPayload.ProcessingTiles = [];
    }

    private async void TryCastCard()
    {
        var manaCost = 0;

        try
        {
            if (SelectedCard is null) return;
            if (_cachedAbilityContext is null)
                BuildAbilityContextPayload();

            // -- Validate --
            var castingPlayer = _gameContext.GameLoopManager.GetActivePlayer();
            var valid = SelectedCard.ValidateCast(
                _cachedAbilityContext!,
                _cachedAbilityPayload.AccumulatedTargets,
                out var simPayload,
                out var reason,
                state: null,
                balance: castingPlayer.Mana
            );

            if (!valid)
            {
                _log.Here().Debug("Card validation failed: {CastFailureReason}", reason);
                PlayerHand.MoveCardTo(SelectedCard, SelectedCard.HandPosition);
                return;
            }

            // -- Cast --

            manaCost = SelectedCard.CardData.Ability.GetCost(_cachedAbilityContext, simPayload);
            _gameContext.GameLoopManager.GetActivePlayer().Mana -= manaCost;

            var castResult = await SelectedCard.CastAsync(
                _cachedAbilityContext,
                _cachedAbilityPayload.AccumulatedTargets,
                null
            );

            if (castResult is null or ErrorResult)
            {
                _log.Here().Debug("Card casting failed: {EffectResult}", castResult);
                _gameContext.GameLoopManager.GetActivePlayer().Mana += manaCost;
                PlayerHand.MoveCardTo(SelectedCard, SelectedCard.HandPosition);
                return;
            }

            // -- Cleanup --
            var castCard = SelectedCard;
            PlayerHand.RemoveCard(castCard);
            DeselectCard(castCard);
            castCard.QueueFree();
        }
        catch (Exception e)
        {
            // -- Rollback --
            _log.Here().Err(e, "Failed to cast card");
            if (manaCost > 0)
                _gameContext.GameLoopManager.GetActivePlayer().Mana += manaCost;

            if (SelectedCard is not null)
                PlayerHand.MoveCardTo(SelectedCard, SelectedCard.HandPosition);
        }
    }
}