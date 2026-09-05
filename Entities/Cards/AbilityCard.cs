#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AKidsDream.Abilities.Effects;
using AKidsDream.Common.Components.TweenComponent.Resources;
using AKidsDream.Common.Errors;
using AKidsDream.Common.Logging;
using AKidsDream.Common.Results;
using AKidsDream.Res.Common.Components.TweenComponent.Resources;
using Godot;
using Serilog;

namespace AKidsDream.Entities.Cards;

// DO not modify this class
[GlobalClass]
[Tool]
public partial class AbilityCard : Control
{
    public CardId Id = CardId.GetNextId();

    [Export] public required AbilityCardData CardData;
    [Export] public required Label CardName;
    [Export] public required Sprite2D CardBackground;
    [Export] public required Sprite2D CardPortrait;
    
    [Export] public required ShaderMaterial SelectionMaterial;
    [Export] public required TweenComponent SelectionTweenComp;
    
    public Vector2 HandPosition { get; set; }
    private Tween? _disablingTween;

    private ILogger _log = GameLogger.For<AbilityCard>();


    private bool _disabled;

    public bool Disabled
    {
        get => _disabled;
        set
        {
            if (_disabled == value) return;

            _disabled = value;
            var targetColor = _disabled ? new Color(0.35f, 0.35f, 0.35f) : new Color(1, 1, 1);

            _disablingTween?.Kill();
            _disablingTween = CreateTween();
            _disablingTween.TweenProperty(CardBackground, "modulate", targetColor, 0.2f);
        }
    }

    private bool _isSelected;

    [Export]
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected.Equals(value)) return;
            
            _isSelected = value;
            
            var tweenAnimation = _isSelected
                ? nameof(TweenAnimationIdentifiers.OnSelectCard)
                : nameof(TweenAnimationIdentifiers.OnDeselectCard);
            SelectionTweenComp.PlayTween(tweenAnimation);

            var shaderMaterial = (ShaderMaterial)CardBackground.Material;
            shaderMaterial.SetShaderParameter("type" , _isSelected ? 1 : 0); // 1 = round, 0 = disabled
            GD.Print(shaderMaterial.GetShaderParameter("Type"));
        }
    }
    
    [ExportToolButton("Set Portrait Scale")]
    private Callable SetPortraitScaleBtn => Callable.From(() =>
    {
        _SetPortraitScale();
        DisplayCard(CardData);
    });

    public override void _Ready()
    {
        _log = _log.ForContext("IdTag", Id)
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
    public async Task<Result<(CompositeOutcome Outcomes, AbilityPayload Payload), CastError>> CastAsync(
        AbilityContext abilityContext,
        List<Vector2I> targetedTiles,
        AbilityState? state = null
    )
    {
        if (CardData.Ability == null)
        {
            _log.Here().Err("Attempted to cast a card without ability data");
            return Result.Fail<(CompositeOutcome Outcomes, AbilityPayload Payload), CastError>(
                new CastError.AbilityNotFound("CardAbility"));
        }
        try
        {
            var castResult = await CardData.Ability.CastAsync(abilityContext, targetedTiles, state);
            if (castResult.IsFailure)
            {
                _log.Here().Err("Card cast failed: {CastError}", castResult.Error);
                return castResult;
            }

            _log.Here().Info("Card cast successfully");
            return castResult;
        }
        catch (Exception e)
        {
            _log.Here().Err(e, "Exception while casting card");
            return Result.Fail<(CompositeOutcome Outcomes, AbilityPayload Payload), CastError>(
                new CastError.EffectFailed(new EffectError.ExecutionFailed(e.Message)));
        }
    }

    /// <summary>
    /// Validates the cast and returns the simulated payload.
    /// Use this to get the payload for custom logic before casting.
    /// </summary>
    public Result<AbilityPayload, CastError> ValidateCast(
        AbilityContext abilityContext,
        List<Vector2I> targetedTiles,
        AbilityState? state = null,
        int? balance = null
    )
    {
        if (CardData.Ability == null)
        {
            _log.Here().Err("Attempted to validate a card without ability data");
            return Result.Fail<AbilityPayload, CastError>(new CastError.AbilityNotFound("CardAbility"));
        }

        var validationResult = CardData.Ability.ValidateCast(
            abilityContext,
            targetedTiles,
            state: state,
            balance: balance);

        return validationResult;
    }

    /// <summary>
    /// Validates and casts the card.
    /// Use this for the standard casting flow without the need to access simulated payloads.
    /// </summary>
    public async Task<Result<(CompositeOutcome Outcomes, AbilityPayload Payload), CastError>> ValidateAndCastAsync(
        AbilityContext abilityContext,
        List<Vector2I> targetedTiles,
        AbilityState? state = null,
        int? balance = null
    )
    {
        var validation = ValidateCast(abilityContext, targetedTiles, state: state, balance: balance);
        if (validation.IsFailure)
        {
            return Result.Fail<(CompositeOutcome, AbilityPayload), CastError>(validation.Error);
        }

        return await CastAsync(abilityContext, targetedTiles, state);
    }
}
