using System.Threading.Tasks;
using AKidsDream.Commands;
using TweenComp.Resources.Blueprint;
using AKidsDream.Utilities.Errors;
using AKidsDream.Utilities.Logging;
using AKidsDream.Utilities.Results;
using AKidsDream.Entities.Cards;
using AKidsDream.Managers.SaveSystems;
using Serilog;

/// <summary>
/// Casts a card. Requires the player to have enough mana to cast.
/// After a successful cast, the player's mana is deducted.'
/// </summary>
/// <param name="card">The <see cref="AbilityCard"/> to cast</param>
/// <param name="abilityContext">The <see cref="GameContext"/></param>
/// <param name="payload">The <see cref="AbilityPayload"/> needed for casting and validation</param>
public class CastCardCommand(
    AbilityCard card,
    AbilityContext abilityContext,
    AbilityPayload payload
) : IAsyncGameBaseCommand
{
    private static readonly ILogger Log = GameLogger.For<CastCardCommand>();

    public async Task<Result<GameError>> ExecuteAsync(GameContext context)
    {
        var castingPlayer = context.GameLoopManager.GetActivePlayer();
        var previousMana = castingPlayer.Mana;

        // Validate cast with current mana balance
        var validationResult = card.ValidateCast(
            abilityContext,
            payload.AccumulatedTargets,
            state: null,
            balance: castingPlayer.Mana
        );

        if (validationResult.IsFailure)
        {
            Log.ForContext("CardIdTag", card.Id)
                .ForContext("CardNameTag", card.CardData.Name)
                .Here().Debug(
                "Card cast validation failed: {CastError}",
                validationResult.Error);
            return validationResult.DropValue().Map<GameError>(error => error);
        }

        var simPayload = validationResult.Value;
        var manaCost = card.CardData.Ability.GetCost(abilityContext, simPayload);

        // Deduct mana before cast
        castingPlayer.Mana -= manaCost;

        var castResult = await card.CastAsync(
            abilityContext,
            simPayload.AccumulatedTargets,
            skipValidation: true,
            balance: castingPlayer.Mana
        );

        if (castResult.IsFailure)
        {
            // Rollback mana on failure
            castingPlayer.Mana = previousMana;
            Log.ForContext("CardIdTag", card.Id)
                .ForContext("CardNameTag", card.CardData.Name)
                .Here().Warn(
                "Card casting failed with error: {CastError}",
                castResult.Error);
            return castResult.DropValue();
        }
        
        Log.ForContext("CardIdTag", card.Id)
            .ForContext("CardNameTag", card.CardData.Name)
            .Here().Info(
            "Casted card at {TargetCount} target(s)",
            payload.ProcessingTiles.Count);

        EventBus.Instance.EmitSignal(EventBus.SignalName.ManaChanged, previousMana, castingPlayer.Mana);

        
        return Result<GameError>.Ok();
    }
}