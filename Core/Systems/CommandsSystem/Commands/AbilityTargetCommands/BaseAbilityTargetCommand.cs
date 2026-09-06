#nullable enable
using System;
using System.Collections.Generic;
using AKidsDream.Common;
using AKidsDream.Common.Components.TweenComponent.Resources;
using AKidsDream.Common.Errors;
using AKidsDream.Common.Logging;
using AKidsDream.Common.Results;
using Godot;
using Serilog;

namespace AKidsDream.Commands;

public abstract class BaseAbilityTargetCommand(Vector2I targetedTile, AbilityContext ctx, AbilityPayload payload)
    : IGameCommand
{
    private static readonly ILogger Log = GameLogger.For<BaseAbilityTargetCommand>();

    protected readonly Vector2I TargetedTile = targetedTile;
    protected readonly AbilityContext Ctx = ctx;
    protected readonly AbilityPayload Payload = payload;

    public Result<GameError> Execute(GameContext context)
    {
        if (Ctx.Caster is not Unit caster)
            throw new InvalidOperationException($"The caster is not a unit. Caster type: {Ctx.Caster.GetType().Name}");

        if (!caster.AbilityC.Abilities.TryGetValue(Ctx.Ability.Name, out var ability))
            return Result<GameError>.Fail(new AbilityError.AbilityNotFound(caster.CasterId, Ctx.Ability.Name));

        var preconditionsResult = ValidatePreconditions();
        if (preconditionsResult.IsFailure)
            return preconditionsResult;

        var modifiedTargets = GetModifiedTargets();

        var validationResult = caster.AbilityC.ValidateCast(
            Ctx.Ability.Name,
            Ctx,
            modifiedTargets,
            skipCostCheck: true);

        if (validationResult.IsFailure)
        {
            return Result<GameError>.Fail(validationResult.Error);
        }

        var resimulatedPayload = validationResult.Value;
        Payload.SetValuesTo(resimulatedPayload);

        Log.Here().Info(
            "{Action} target {TargetTile} for ability '{AbilityName}'",
            GetActionName(),
            TargetedTile,
            Ctx.Ability.Name,
            Ctx.Caster.CasterName,
            Ctx.Caster.CasterId);

        context.AbilityVisualizer.ShowEffectVisualization(Ctx, resimulatedPayload, ability.Effects);
        context.AbilityVisualizer.ShowReachVisualization(Ctx, resimulatedPayload, ability);

        return Result<GameError>.Ok();
    }

    protected virtual Result<GameError> ValidatePreconditions() => Result<GameError>.Ok();
    protected abstract List<Vector2I> GetModifiedTargets();
    protected abstract string GetActionName();
}
