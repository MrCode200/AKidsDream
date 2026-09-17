using System;
using System.Collections.Generic;
using AKidsDream.Common.Logging;
using AKidsDream.Core.Teams;
using AKidsDream.Managers.SaveSystems;
using AKidsDream.Utilities;
using Godot;
using Serilog;
using Vector2 = Godot.Vector2;

namespace AKidsDream.Entities.Cards;

public partial class PlayerHand : Node2D
{
    [ExportCategory("Card Settings")] [Export(PropertyHint.Range, "0, 10")]
    public int MaxHandSize;

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

    public PlayerData ActivePlayer;
    public readonly List<AbilityCard> Hand = [];
    private Vector2 _cardSize;
    private Vector2 _centerScreen;
    private float _handYPosition;
    private float _maxHandWidth;

    private static ILogger _log = GameLogger.For(typeof(PlayerHand));

    public override void _Ready()
    {
        var screenSize = GetViewport().GetVisibleRect().Size;
        _centerScreen = screenSize / 2;
        _handYPosition = screenSize.Y * HandYScreenRatio;
        _maxHandWidth = screenSize.X * MaxHandWidthScreenRatio;

        var tempCard = CardPrefab.Instantiate<AbilityCard>();
        _cardSize = tempCard.Size * tempCard.Scale;
        tempCard.QueueFree();
    }

    public void LoadPlayerHand(PlayerData playerData)
    {
        ActivePlayer = playerData;
        _log = _log.ForContext("IdTag", playerData.PlayerId)
            .ForContext("NameTag", playerData.Name);

        HideHand();
        Hand.Clear();

        foreach (var unit in playerData.PlayerHand)
        {
            AddCard(unit, false);
        }

        RefreshHandLayout();
        _log.Here().Info("Loaded hand for player {PlayerId}, hand size: {HandSize}", playerData.PlayerId, Hand.Count);
    }

    public void ShowHand()
    {
        RefreshHandLayout();
    }

    public void HideHand()
    {
        var i = 0;
        foreach (var card in Hand)
        {
            if (card.IsSelected)
                card.IsSelected = false;
            card.MoveToHand(CardSpawnPoint, i * 0.025f);
            i++;
        }
    }

    public void DrawCards(int count)
    {
        count = Math.Min(count, MaxHandSize - Hand.Count);

        _log.Here().Debug("Drawing {CardCount} cards for player {PlayerId}", count, ActivePlayer?.PlayerId);

        for (var i = 0; i < count; i++)
        {
            AddCard();
        }

        RefreshHandLayout();
    }

    public void AddCard(Global.UnitName unitName = Global.UnitName.Unassigned, bool updatePlayerData = true)
    {
        unitName = unitName == Global.UnitName.Unassigned ? GetRandomUnitName() : unitName;
        var cardPath = Utils.GetCardPath(unitName);
        var cardData = (AbilityCardData)ResourceLoader.Load(cardPath);

        var newCard = CardPrefab.Instantiate<AbilityCard>();

        newCard.DisplayCard(cardData);
        newCard.Position = CardSpawnPoint;

        AddChild(newCard);
        Hand.Add(newCard);

        _log.Here().Debug("Added card {UnitName} (id: {CardId}) to hand, hand size: {HandSize}", unitName, newCard.Id,
            Hand.Count);

        if (updatePlayerData)
            ActivePlayer.PlayerHand.Add(unitName);
    }

    private Global.UnitName GetRandomUnitName()
    {
        var unitNames = Enum.GetNames<Global.UnitName>();
        return Enum.Parse<Global.UnitName>(unitNames[GD.RandRange(1, unitNames.Length - 1)]);
    }

    public void RemoveCard(AbilityCard card)
    {
        if (!Hand.Contains(card)) return;

        var unitName = Enum.Parse<Global.UnitName>(card.CardData.Name);
        Hand.Remove(card);
        ActivePlayer.PlayerHand.Remove(unitName);
        RefreshHandLayout();

        _log.Here().Debug("Removed card {UnitName} (id: {CardId}) from hand, hand size: {HandSize}", unitName, card.Id,
            Hand.Count);
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
            Hand[i].MoveToHand(null, i * 0.025f);
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

    public void SyncCardZOrder()
    {
        AbilityCard selectedCard = null;
        for (var i = 0; i < Hand.Count; i++)
        {
            var card = Hand[i];
            if (card.IsSelected) selectedCard = card;
            if (card.GetIndex() != i)
            {
                MoveChild(card, i);
            }
        }

        if (selectedCard != null)
            MoveChild(selectedCard, -1);
    }
}