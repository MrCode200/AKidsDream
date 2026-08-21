#nullable enable
using System.Collections.Generic;
using AKidsDream.Common.Logging;
using AKidsDream.Managers.SaveSystems;
using AKidsDream.Units.Resources.Components;
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
        if (!Ctx.Caster.AbilityC.Abilities.TryGetValue(Ctx.Ability.Name, out var ability))
            return CommandResult.Fail(this, CommandFailureType.AbilityNotFound,
                $"Ability '{Ctx.Ability.Name}' not found.");

        var preconditionsResult = ValidatePreconditions();
        if (preconditionsResult != null)
            return preconditionsResult;

        var modifiedTargets = GetModifiedTargets();

        if (!Ctx.Caster.AbilityC.ValidateCast(
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
            Ctx.Caster.UnitName,
            Ctx.Caster.UnitId);

        context.AbilityVisualizer.ShowEffectVisualization(Ctx, resimulatedPayload, ability.Effects);
        context.AbilityVisualizer.ShowReachVisualization(Ctx, resimulatedPayload, ability);

        return CommandResult.Ok(this);
    }

    protected virtual CommandResult? ValidatePreconditions() => null;
    protected abstract List<Vector2I> GetModifiedTargets();
    protected abstract string GetActionName();
}