using System.Threading.Tasks;
using AKidsDream.Commands;
using AKidsDream.Common.Components.TweenComponent.Resources;
using AKidsDream.Common.Errors;
using AKidsDream.Common.Logging;
using AKidsDream.Common.Results;
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
            Log.ForContext<CastCardCommand>().Here().Debug(
                "Card '{CardName}' (id: {CardId}) cast validation failed: {CastError}",
                card.CardData.Name,
                card.Id,
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
            Log.ForContext<CastCardCommand>().Here().Warn(
                "Card '{CardName}' (id: {CardId}) casting failed with error: {CastError}",
                card.CardData.Name,
                card.Id,
                castResult.Error);
            return castResult.DropValue();
        }
        
        Log.ForContext<CastCardCommand>().Here().Info(
            "Casted card '{CardName}' (id: {CardId}) at {TargetCount} target(s)",
            card.CardData.Name,
            card.Id,
            payload.ProcessingTiles.Count);

        EventBus.Instance.EmitSignal(EventBus.SignalName.ManaChanged, previousMana, castingPlayer.Mana);

        
        return Result<GameError>.Ok();
    }
}