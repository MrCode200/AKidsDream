using System;
using System.Collections.Generic;
using Godot;

namespace AKidsDream.Entities.Cards;

public partial class PlayerHand : Node2D
{
    
    [ExportCategory("Card Settings")]
    [Export(PropertyHint.Range, "0, 10")] public int PlayerHandSize;
    [Export(PropertyHint.Range, "0, 100")] public float CardSpacing = 10f;
    [Export] public Vector2 CardSpawnPoint;
    
    [Export(PropertyHint.Range, "0, 100")] public float SelectedCardHeightDelta = 25f;
    [Export(PropertyHint.Range, "0, 1")] public float HandYScreenRatio;
    [Export(PropertyHint.Range, "0, 1")] public float MaxHandWidthScreenRatio;
    
    [ExportGroup("Dependencies")]
    [Export] public PackedScene CardPrefab;
    [Export] public AbilityCardData CardData; // make later card pool
    
    public readonly List<AbilityCard> Hand = [];
    private readonly Dictionary<AbilityCard, Tween> _activeTweens = [];
    private Vector2 _cardSize;
    private Vector2 _centerScreen;
    private float _handYPosition;
    private float _maxHandWidth;
    
    public override void _Ready()
    {
        var screenSize = GetViewport().GetVisibleRect().Size;
        _centerScreen = screenSize / 2;
        _handYPosition = screenSize.Y * HandYScreenRatio;
        _maxHandWidth = screenSize.X * MaxHandWidthScreenRatio;
        
        var tempCard = CardPrefab.Instantiate<AbilityCard>();
        _cardSize = tempCard.Size;
        tempCard.QueueFree();
        
        DrawCards(PlayerHandSize);
    }

    public void ShowHand()
    {
        RefreshHandLayout();
    }

    public void HideHand() // TODO: what func name should UpdateCardPos take?
    {
        foreach (var card in Hand)
            MoveCardTo(card, CardSpawnPoint);
    }

    public void DrawCards(int count)
    {
        for (var i = 0; i < count; i++)
        {
            var newCard = CardPrefab.Instantiate<AbilityCard>();
            newCard.DisplayCard(CardData);
            newCard.Position = CardSpawnPoint;
            
            AddChild(newCard);
            Hand.Add(newCard);
        }

        RefreshHandLayout();
    }

    public void RemoveCard(AbilityCard card)
    {
        if (!Hand.Contains(card)) return;
        
        Hand.Remove(card);
        RefreshHandLayout();
    }

    private void RefreshHandLayout()
    {
        var cardWidth = _cardSize.X + CardSpacing;
        var totalWidth = Hand.Count * cardWidth;
        float? overflow = null;
        if (totalWidth > _maxHandWidth)
        {
            overflow = totalWidth - _maxHandWidth;
            cardWidth -= overflow.Value / Hand.Count;
            totalWidth = _maxHandWidth;
        }

        var index = 0;
        foreach (var card in Hand)
        {
             var xOffset = _centerScreen.X + index * cardWidth - totalWidth / 2;
             xOffset -= overflow != null ? CardSpacing / 2 : 0; // Simulate Spacing from both sides
             var newPos = new Vector2(xOffset, _handYPosition);
             
             card.HandPosition = newPos;
             MoveCardTo(card, newPos);
             
             index++;
        }
    }
    
    public void MoveCardTo(AbilityCard card, Vector2 newPosition)
    {
        if (_activeTweens.TryGetValue(card, out var existingTween))
        {
            existingTween.Kill();
            _activeTweens.Remove(card);
        }
        
        if (card.IsSelected)
            newPosition -= new Vector2(0, SelectedCardHeightDelta);

        var tween = CreateTween();
        tween.TweenProperty(card, "position", newPosition, 0.5f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Linear);
        
        _activeTweens[card] = tween;
        tween.TweenCallback(Callable.From(() => _activeTweens.Remove(card)));
    }
}