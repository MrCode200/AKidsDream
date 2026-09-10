using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Godot;
using Vector2 = Godot.Vector2;

namespace AKidsDream.Entities.Cards;

public partial class PlayerHand : Node2D
{
    [ExportCategory("Card Settings")] [Export(PropertyHint.Range, "0, 10")]
    public int PlayerHandSize;

    [Export] public Curve CardPositionCurve;
    [Export] public float CardSpacing = -30f;
    [Export] public Vector2 CardSpawnPoint;

    [Export(PropertyHint.Range, "0, 100")] public float SelectedCardHeightDelta = 25f;
    [Export] public float HandUpperBoundDeltaHeight; // Using the curve the height from the curve value 0 to 1
    [Export(PropertyHint.Range, "0, 1")] public float HandYScreenRatio; // The base (curve = 0)
    [Export] public float CardRotationHeightUnderHandY;
    [Export(PropertyHint.Range, "0, 1")] public float MaxHandWidthScreenRatio;
    [Export] public bool UseDelayedHandTween;

    [ExportGroup("Dependencies")] [Export] public PackedScene CardPrefab;
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
        var i = 0;
        foreach (var card in Hand)
        {
            MoveCardTo(card, CardSpawnPoint, i * 0.025f);
            i++;
        }        
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
        for (var i = 0; i < Hand.Count; i++)
        {
            var pos = GetCardPosition(i);
            Hand[i].HandPosition = pos;

            var rot = GetCardRotation(pos, new Vector2(
                _centerScreen.X,
                _handYPosition + CardRotationHeightUnderHandY)
            );
            Hand[i].HandRotation = rot;
            MoveCardTo(Hand[i], delay: i * 0.025f);
        }
    }

    private Vector2 GetCardPosition(int index)
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

        var xOffset = _centerScreen.X + index * cardWidth - totalWidth / 2f;
        xOffset -= overflow != null ? CardSpacing / 2f : 0f;
        var yOffset = _handYPosition
                      - CardPositionCurve.Sample((index + 0.5f) / Hand.Count) * HandUpperBoundDeltaHeight;

        return new Vector2(xOffset, yOffset);
    }

    private float GetCardRotation(Vector2 cardPosition, Vector2 cardRotationPivot)
    {
        var dx = cardPosition.X - cardRotationPivot.X;
        var dy = cardRotationPivot.Y - cardPosition.Y;
        var angle = Math.Atan2(dy, dx) * 180f / Math.PI;
        return 90 - (float)angle;
    }

    /// <summary>
    /// Move the card to a new position and rotation.
    /// </summary>
    /// <param name="card">The card to move</param>
    /// <param name="newPosition">If newPosition is null, the card will be moved to its default HandPosition</param>
    /// <param name="newRotation">If rotation is null, the card will be rotated to its default HandRotation</param>
    public void MoveCardTo(AbilityCard card, Vector2? newPosition = null, float delay = 0)
    {
        if (_activeTweens.TryGetValue(card, out var existingTween))
        {
            existingTween.Kill();
            _activeTweens.Remove(card);
        }
        
        if (!UseDelayedHandTween) delay = 0;

        var targetPosition = newPosition ?? card.HandPosition;

        if (card.IsSelected)
            targetPosition -= new Vector2(0, SelectedCardHeightDelta);

        var tween = CreateTween();
        tween.SetParallel();

        tween.TweenProperty(card, "position", targetPosition, 0.6f)
            .SetEase(Tween.EaseType.InOut)
            .SetTrans(Tween.TransitionType.Back)
            .SetDelay(delay);

        
        tween.TweenProperty(card, "rotation_degrees",
                card.IsSelected ? 0 : card.HandRotation,
                0.25f
            )
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Back)
            .SetDelay(0.1f);
        
        // A failed attempt of a cooler animation
        /* 
        var updatedStartingRot = false;
        float fromProgress = 0;
        var from = card.RotationDegrees;
        var toHandPosRotation = GetCardRotation(card.Position, card.HandPosition);
        tween.TweenMethod(Callable.From((float progress) =>
            {
                if (progress > 0.75 && !updatedStartingRot && card.IsSelected)
                {
                    from = card.RotationDegrees;
                    fromProgress = progress;
                    updatedStartingRot = true;
                }
                var to = progress <= 0.75 ? toHandPosRotation : (card.IsSelected ? 0 : card.HandRotation);
                var rot = Mathf.Lerp(from, to, (progress - fromProgress) / (1 - fromProgress));
                GD.Print($"From: {from} To: {to} Rot: {rot} Progress: {progress}");
                card.RotationDegrees = rot;
            }), 0f, 1f, 0.5f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Sine)
            .SetDelay(0.1f);
*/
        _activeTweens[card] = tween;
        tween.TweenCallback(Callable.From(() => _activeTweens.Remove(card)));
    }
    
    public bool IsTweening(AbilityCard card) => _activeTweens.ContainsKey(card);
}