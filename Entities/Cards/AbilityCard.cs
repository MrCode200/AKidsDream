#nullable enable
using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using AKidsDream.Abilities.Effects;
using AKidsDream.Common.Components.TweenComponent.Resources;
using AKidsDream.Common.Logging;
using AKidsDream.Core.Managers;
using Godot.Collections;
using Serilog;

namespace AKidsDream.Entities.Cards;

[Tool]
[Icon("res://Entities/Cards/Assets/card-icon.png")]
public partial class AbilityCard : Control, IBlockable
{
    [Export] public required AbilityCardData CardData;
    [Export] public required Label CardName;
    [Export] public required Sprite2D CardBackground;
    [Export] public required Sprite2D CardPortrait;
    [Export] public Array<BlockingStrategy> BlockingStrategies { get; set; } =
        [BlockingStrategy.BlockOnBlockingTrigger, BlockingStrategy.BlockOnEffectApply];
    public bool IsBlocked { get; set; }

    [Export] public required ShaderMaterial SelectionMaterial;

    public Vector2 HandPosition;
    private readonly ILogger _log = GameLogger.For<AbilityCard>();

    private bool _isSelected;

    [Export]
    public bool IsSelected
    {
        get => _isSelected;
        set => _isSelected = value;
    }

    public CardId Id = CardId.GetNextId();

    [ExportToolButton("Set Portrait Scale")]
    private Callable SetPortraitScaleBtn => Callable.From(() =>
    {
        _SetPortraitScale();
        DisplayCard(CardData);
    });

    public override void _Ready()
    {
        _log.ForContext("IdTag", Id)
            .ForContext("NameTag", CardData.Name + "Card");

        CardBackground.Material = SelectionMaterial;
        _SetPortraitScale();
    }

    private void _SetPortraitScale(float padding = 5f)
    {
        Vector2 portraitSize = CardPortrait.Texture.GetSize();
        Vector2 availableSize = CardBackground.Texture.GetSize();

        availableSize -= new Vector2(padding, padding);

        var fitScale = Math.Min(
            availableSize.X / portraitSize.X,
            availableSize.Y / portraitSize.Y
        );
        CardPortrait.Scale = Vector2.One * fitScale;
    }

    // -- LOGIC --
    public void DisplayCard(AbilityCardData cardData)
    {
        CardPortrait.Texture = cardData.Ability.Icon;
        CardName.Text = cardData.Name;
    }

    /// <summary>
    /// Pure async cast method - executes the ability without validation or cost deduction.
    /// Use this if you want to handle validation and cost logic separately.
    /// </summary>
    public async Task<EffectResult?> CastAsync(
        AbilityContext abilityContext,
        List<Vector2I> targetedTiles,
        AbilityState? state = null
    )
    {
        if (CardData.Ability == null)
        {
            _log.Here().Err("Attempted to cast a card without ability data");
            return null;
        }

        try
        {
            var (result, _) = await CardData.Ability.CastAsync(abilityContext, targetedTiles, state);
            if (result is ErrorResult er)
            {
                _log.Here().Err($"Card cast failed: {er.Error}");
                return er;
            }

            GD.Print(result);
            _log.Here().Info($"Card cast successfully");
            return result;
        }
        catch (Exception e)
        {
            _log.Here().Err("Exception while casting card: {Exception}", e);
            return null;
        }
    }

    /// <summary>
    /// Validates the cast and returns the simulated payload.
    /// Use this to get the payload for custom logic before casting.
    /// </summary>
    public bool ValidateCast(
        AbilityContext abilityContext,
        List<Vector2I> targetedTiles,
        [NotNullWhen(true)] out AbilityPayload? payload,
        out CastFailureReason reason,
        AbilityState? state = null,
        int? balance = null
    )
    {
        payload = null;
        reason = CastFailureReason.None;

        if (CardData.Ability == null)
        {
            _log.Here().Err("Attempted to validate a card without ability data");
            reason = CastFailureReason.AbilityNotFound;
            return false;
        }

        if (!CardData.Ability.ValidateCast(
                abilityContext,
                targetedTiles,
                out payload,
                out reason,
                state: state,
                balance: balance))
        {
            _log.Here().Debug("Casting Card validation failed: {CastFailureReason}", reason);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Validates and casts the card.
    /// Use this for the standard casting flow without the need to access simulated payloads.
    /// </summary>
    public async Task<EffectResult?> ValidateAndCastAsync(
        AbilityContext abilityContext,
        List<Vector2I> targetedTiles,
        AbilityState state,
        int? balance = null
    )
    {
        if (!ValidateCast(
                abilityContext,
                targetedTiles,
                out _,
                out _,
                state: state,
                balance: balance))
        {
            return null;
        }
        
        return await CastAsync(abilityContext, targetedTiles, state);
    }
    
    public void SetBlocked(bool block)
    {
        IsBlocked = block;
    }
}