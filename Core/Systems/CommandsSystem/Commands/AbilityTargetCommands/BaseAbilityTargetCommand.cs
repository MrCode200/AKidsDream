#nullable enable
using System.Collections.Generic;
using AKidsDream.Common;
using AKidsDream.Common.Logging;
using AKidsDream.Common.Components.TweenComponent.Resources;
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
    
    public CommandResult Execute(GameContext context)
    {
        if (Ctx.Caster is not Unit caster)
           return CommandResult.Fail(this, CommandFailureType.NullArgument, "The caster is not a unit.");
        
        if (!caster.AbilityC.Abilities.TryGetValue(Ctx.Ability.Name, out var ability))
            return CommandResult.Fail(this, CommandFailureType.AbilityNotFound,
                $"Ability '{Ctx.Ability.Name}' not found.");

        var preconditionsResult = ValidatePreconditions();
        if (preconditionsResult != null)
            return preconditionsResult;

        var modifiedTargets = GetModifiedTargets();

        if (!caster.AbilityC.ValidateCast(
                Ctx.Ability.Name,
                Ctx,
                modifiedTargets,
                out var resimulatedPayload,
                out var failureReason,
                skipCostCheck: true)
           )
        {
            return CommandResult.Fail(
                this,
                CommandFailureMapper.MapCastFailureToCommandFailure(failureReason),
                $"Validation failed: {failureReason}"
            );
        }

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

        return CommandResult.Ok(this);
    }

    protected virtual CommandResult? ValidatePreconditions() => null;
    protected abstract List<Vector2I> GetModifiedTargets();
    protected abstract string GetActionName();
}