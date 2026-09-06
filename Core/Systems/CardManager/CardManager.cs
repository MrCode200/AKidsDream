#nullable enable
using System;
using AKidsDream.Commands;
using AKidsDream.Common;
using AKidsDream.Common.Components.TweenComponent.Resources;
using AKidsDream.Common.Logging;
using AKidsDream.Entities.Cards;
using AKidsDream.GameBoard;
using AKidsDream.Managers;
using AKidsDream.Managers.SaveSystems;
using Godot;
using Godot.Collections;
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
public partial class CardManager : Node2D, IBlockable
{
    [Export(PropertyHint.InputName, "show_builtin")]
    public required string SelectionInputName;

    [Export] public float DragThreshold = 10f;

    [ExportGroup("Dependencies")] [Export] public required AbilityVisualizer AbilityVisualizer;
    [Export] public required PlayerHand PlayerHand;

    public AbilityCard? SelectedCard { get; private set; }

    public bool IsBlocked { get; set; }

    public Array<BlockingStrategy> BlockingStrategies { get; set; } =
        [BlockingStrategy.BlockOnBlockingTrigger, BlockingStrategy.BlockOnEffectApply];

    private bool _isDragging;
    private bool _isPressed;
    private bool _isCasting;

    private AbilityCard? _pressedCard;
    private Vector2? _cardDraggingAnchor;
    private Vector2 _pressPosition;
    private Vector2I? _lastTargetTile;

    private GameContext _gameContext = null!;
    private AbilityContext? _cachedAbilityContext;
    private AbilityPayload _cachedAbilityPayload = null!;

    private readonly ILogger _log = GameLogger.For<CardManager>();

    // -- GODOT LIFECYCLE --

    public override void _Ready()
    {
        BlockingManager.Instance.Register(this);
        _cachedAbilityPayload = new AbilityPayload
        {
            CurrentOrigin = null,
            State = new AbilityState(),
            AccumulatedTargets = [],
            ProcessingTiles = []
        };

        EventBus.Instance.UnitSelected += OnUnitSelected;
        EventBus.Instance.UnitDeselected += OnUnitDeselected;
    }

    public override void _ExitTree()
    {
        BlockingManager.Instance.Unregister(this);
        EventBus.Instance.UnitSelected -= OnUnitSelected;
        EventBus.Instance.UnitDeselected -= OnUnitDeselected;
    }

    private void OnUnitDeselected(Unit unit)
    {
        PlayerHand.ShowHand();
    }

    private void OnUnitSelected(Unit unit)
    {
        if (SelectedCard is not null)
            DeselectCard(SelectedCard);

        PlayerHand.HideHand();
    }

    public void Init(GameContext gameContext)
    {
        _gameContext = gameContext;
    }

    public override void _Input(InputEvent @event)
    {
        if (string.IsNullOrEmpty(SelectionInputName) || _isCasting) return;

        // Handle PRESS: record anchor and pressed card
        if (Input.IsActionJustPressed(SelectionInputName))
        {
            var hoveredControl = GetViewport().GuiGetHoveredControl();
            _pressedCard = hoveredControl as AbilityCard;
            _pressPosition = GetViewport().GetMousePosition();
            _isDragging = false;
            _isPressed = true;
            _cardDraggingAnchor = null;
        }
        // Handle RELEASE: trigger cast if dragging, or handle click selection
        else if (@event.IsActionReleased(SelectionInputName))
        {
            _isPressed = false;

            if (_isDragging)
            {
                _isDragging = false;
                _cardDraggingAnchor = null;
                _lastTargetTile = null;
                TryCastCard();
            }
            else
            {
                HandleCardClick(_pressedCard);
            }

            _pressedCard = null;
        }
        // Handle MOTION / DRAG THRESHOLD
        else if (
            _isPressed &&
            !IsBlocked &&
            !_isDragging &&
            _pressedCard is not null &&
            @event is InputEventMouseMotion mouseMotion
        )
        {
            var distance = _pressPosition.DistanceTo(mouseMotion.Position);
            if (!(distance >= DragThreshold)) return;

            _isDragging = true;
            _cardDraggingAnchor = mouseMotion.Position - _pressedCard.Position;

            if (SelectedCard is null || SelectedCard != _pressedCard)
            {
                HandleCardClick(_pressedCard);
                SelectedCard?.SelectionTweenComp.KillTween();
            }
            else
                BuildAbilityContextPayload();
        }
    }

    public override void _Process(double delta)
    {
        if (SelectedCard is null || !_isDragging || _cardDraggingAnchor is null || IsBlocked) return;

        MoveCardToMousePos();
        UpdateTargetTileVisualization();
    }

    // -- SELECTION STATE MANAGEMENT --

    private void HandleCardClick(AbilityCard? clickedCard)
    {
        if (clickedCard is null || IsBlocked) return;

        if (SelectedCard is null)
        {
            SelectCard(clickedCard);
        }
        else if (SelectedCard.Id == clickedCard.Id)
        {
            DeselectCard(clickedCard);
        }
        else
        {
            ChangeCard(clickedCard);
        }
    }

    private void DeselectCard(AbilityCard cardToDeselect)
    {
        if (cardToDeselect.Id != SelectedCard?.Id)
        {
            //_log.Here().Warn("Tried to deselect card {CardId} when selected card is {SelectedCardId}",
            //    cardToDeselect.Id, SelectedCard?.Id);
            return;
        }
        
        cardToDeselect.IsSelected = false;
        SelectedCard = null;
        _cardDraggingAnchor = null;
        _lastTargetTile = null;
        
        ClearAbilityContextPayload();
        AbilityVisualizer.ClearTilemaps();
        
        _log.Here().Debug("Deselected card {CardName} (id: {CardId})", cardToDeselect.CardData.Name, cardToDeselect.Id);

        EventBus.Instance.EmitSignal(EventBus.SignalName.CardDeselected, cardToDeselect);
    }

    private void SelectCard(AbilityCard cardToSelect)
    {
        SelectedCard = cardToSelect;
        SelectedCard.IsSelected = true;
        BuildAbilityContextPayload();

        AbilityVisualizer.ShowReachVisualization(
            _cachedAbilityContext!,
            _cachedAbilityPayload,
            SelectedCard.CardData.Ability
        );

        _log.ForContext("NameTag", cardToSelect.CardData.Name)
            .ForContext("IdTag", cardToSelect.Id)
            .Here().Debug("Card selected {NameTag} (id: {IdTag})");

        EventBus.Instance.EmitSignal(EventBus.SignalName.CardSelected, cardToSelect);
    }

    private void ChangeCard(AbilityCard newCard)
    {
        if (SelectedCard is null || SelectedCard.Id == newCard.Id)
        {
            _log.Here().Warn("Tried to change from card:{SelectedCardId} to clickedCard:{ClickedCardId}",
                SelectedCard?.Id, newCard.Id);
            return;
        }

        var oldCard = SelectedCard;
        oldCard.IsSelected = false;

        SelectedCard = newCard;
        SelectedCard.IsSelected = true;

        BuildAbilityContextPayload();
        AbilityVisualizer.ShowReachVisualization(
            _cachedAbilityContext!,
            _cachedAbilityPayload,
            SelectedCard.CardData.Ability
        );

        _log.Here().Debug("Switched selection from {OldCardName}:{OldIdTag} to {NewCardName}:{NewIdTag}",
            oldCard.CardData.Name, oldCard.Id, newCard.CardData.Name, newCard.Id);

        EventBus.Instance.EmitSignal(EventBus.SignalName.CardChanged, oldCard, SelectedCard);
    }

    // -- DRAGGING & TARGETING --

    private void MoveCardToMousePos()
    {
        if (SelectedCard is null || _cardDraggingAnchor is null) return;

        var viewportSize = GetViewport().GetVisibleRect().Size;
        var mousePos = GetViewport().GetMousePosition();
        var targetPos = mousePos - _cardDraggingAnchor.Value;

        SelectedCard.Position = new Vector2(
            Mathf.Clamp(targetPos.X, 0, viewportSize.X - SelectedCard.Size.X),
            Mathf.Clamp(targetPos.Y, 0, viewportSize.Y - SelectedCard.Size.Y)
        );
    }

    private void UpdateTargetTileVisualization()
    {
        if (SelectedCard is null || _cachedAbilityContext is null) return;

        var mousePos = GetGlobalMousePosition();
        var mouseTile = Board.WorldPositionToTilePosition(mousePos);

        if (_lastTargetTile.HasValue && _lastTargetTile.Value == mouseTile) return;

        _lastTargetTile = mouseTile;
        _cachedAbilityPayload.AccumulatedTargets = [mouseTile];
        _cachedAbilityPayload.ProcessingTiles = [mouseTile];

        AbilityVisualizer.ShowEffectVisualization(
            _cachedAbilityContext,
            _cachedAbilityPayload,
            SelectedCard.CardData.Ability.Effects
        );
    }

    // -- ABILITY CONTEXT & CASTING --

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
            Caster = cardCaster,
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
        if (SelectedCard is null || _isCasting) return;

        var castingCard = SelectedCard;
        if (_cachedAbilityContext is null)
            BuildAbilityContextPayload();

        if (_cachedAbilityContext is null) return;

        _isCasting = true;
        var hasCastStarted = false;
        var isSuccess = false;

        try
        {
            var castingPlayer = _gameContext.GameLoopManager.GetActivePlayer();
            var validationResult = castingCard.ValidateCast(
                _cachedAbilityContext,
                _cachedAbilityPayload.AccumulatedTargets,
                state: null,
                balance: castingPlayer.Mana
            );

            if (validationResult.IsFailure)
            {
                _log.Here().Debug("Card {CardName} cast validation failed: {CastError}",
                    castingCard.CardData.Name, validationResult.Error);
                PlayerHand.MoveCardTo(castingCard, castingCard.HandPosition);
                AbilityVisualizer.ClearEffectTilemap();
                return;
            }

            var simPayload = validationResult.Value;

            // -- Cast start --
            hasCastStarted = true;
            EventBus.Instance.EmitSignal(EventBus.SignalName.AbilityCastStart, default(Variant),
                castingCard.CardData.Ability);

            var castResult = await castingCard.CastAsync(
                _cachedAbilityContext,
                _cachedAbilityPayload.AccumulatedTargets
            );

            if (castResult.IsFailure)
            {
                _log.Here().Warn("Card {CardName} casting failed with error: {CastError}",
                    castingCard.CardData.Name, castResult.Error);
                return;
            }

            // Commit state changes atomically on success
            var manaCost = castingCard.CardData.Ability.GetCost(_cachedAbilityContext, simPayload);
            castingPlayer.Mana -= manaCost;

            isSuccess = true;
            PlayerHand.RemoveCard(castingCard);
            DeselectCard(castingCard);
            castingCard.QueueFree();
        }
        catch (Exception e)
        {
            _log.Here().Err(e, "Exception while attempting to cast card {CardName}", castingCard.CardData.Name);
        }
        finally
        {
            // Return card to hand position if not successfully cast and freed
            if (!isSuccess && IsInstanceValid(castingCard) && !castingCard.IsQueuedForDeletion())
            {
                PlayerHand.MoveCardTo(castingCard, castingCard.HandPosition);
                AbilityVisualizer.ClearEffectTilemap();
            }

            if (hasCastStarted)
            {
                EventBus.Instance.EmitSignal(
                    EventBus.SignalName.AbilityCastEnd,
                    default(Variant),
                    castingCard.CardData.Ability
                );
            }

            _isCasting = false;
        }
    }

    // -- BLOCKING --
    public void UpdateCardDisable(bool disable)
    {
        foreach (var card in PlayerHand.Hand)
        {
            card.Disabled = disable;
        }
    }

    public void SetBlocked(bool block)
    {
        IsBlocked = block;
        UpdateCardDisable(block);
        if (SelectedCard is not null)
            DeselectCard(SelectedCard);
    }
}