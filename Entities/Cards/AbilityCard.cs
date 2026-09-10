#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AKidsDream.Abilities.Effects;
using AKidsDream.Common.Components.TweenComponent.Resources;
using AKidsDream.Common.Errors;
using AKidsDream.Common.Logging;
using AKidsDream.Common.Results;
using AKidsDream.Core.Managers.Audio;
using AKidsDream.Res.Common.Components.TweenComponent.Resources;
using AKidsDream.Util.Identifiers;
using AKidsDream.Utilities;
using AKidsDream.Utilities.TypeExtensions;
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
    public float HandRotation { get; set; }
    private int _childSelfIndex;
    private Tween? _rotationTween;
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
            
            if (_isSelected)
            {
                _childSelfIndex = GetIndex();
                GetParent()?.MoveChild(this, -1);
            }
            else if (_childSelfIndex != GetIndex())
            {
                GetParent()?.MoveChild(this, _childSelfIndex);
            }
            
            var tweenAnimation = _isSelected
                ? nameof(TweenAnimationIdentifiers.OnSelectCard)
                : nameof(TweenAnimationIdentifiers.OnDeselectCard);
            SelectionTweenComp.PlayTween(tweenAnimation);
            
            _rotationTween?.Kill();
            _rotationTween = CreateTween();
            _rotationTween.TweenProperty(this, "rotation_degrees", _isSelected ? 0 : HandRotation, 0.2f)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Quint);
            
            AudioManager.Instance.PlayAudio(SoundEffectType.CardSelected);

            var shaderMaterial = (ShaderMaterial)CardBackground.Material;
            shaderMaterial.SetShaderParameter("type" , _isSelected ? 1 : 0); // 1 = round, 0 = disabled
        }
    }
    
    [ExportToolButton("Set Portrait Scale")]
    private Callable SetPortraitScaleBtn => Callable.From(() =>
    {
        CardPortrait.ScaleToMatch(CardBackground, 5f);
        DisplayCard(CardData);
    });

    public override void _Ready()
    {
        _log = _log.ForContext("IdTag", Id)
            .ForContext("NameTag", CardData.Name + "Card");

        CardBackground.Material = SelectionMaterial;
        CardPortrait.ScaleToMatch(CardBackground, 5f);
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
    public async Task<Result<(CompositeOutcome Outcomes, AbilityPayload Payload), GameError>> CastAsync(
        AbilityContext abilityContext,
        List<Vector2I> targetedTiles,
        AbilityState? state = null,
        bool skipValidation = false,
        int? balance = null
    )
    {
        try
        {
            if (!skipValidation)
            {
                var validation = ValidateCast(abilityContext, targetedTiles, state: state, balance: balance);
                if (validation.IsFailure)
                {
                    return Result.Fail<(CompositeOutcome, AbilityPayload), GameError>(validation.Error);
                }
            }
            
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
            return Result.Fail<(CompositeOutcome Outcomes, AbilityPayload Payload), GameError>(
                new UnexpectedError(e));
        }
    }

    /// <summary>
    /// Validates the cast and returns the simulated payload.
    /// Use this to get the payload for custom logic before casting.
    /// </summary>
    public Result<AbilityPayload, AbilityError> ValidateCast(
        AbilityContext abilityContext,
        List<Vector2I> targetedTiles,
        AbilityState? state = null,
        int? balance = null
    )
    {
        var validationResult = CardData.Ability.ValidateCast(
            abilityContext,
            targetedTiles,
            state: state,
            balance: balance);

        return validationResult;
    }
}
