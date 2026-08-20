using System.Linq;
using AKidsDream.Common.Logging;
using AKidsDream.Units.Resources.Components;
using Godot;
using Serilog;

namespace AKidsDream.Commands;

public class RmvAbilityTargetCommand(
    Vector2I targetedTile,
    AbilityContext ctx,
    AbilityPayload payload
) : IGameCommand
{
    private static readonly ILogger Log = GameLogger.For<RmvAbilityTargetCommand>();
    
    public CommandResult Execute(GameContext context)
    {
        if (!ctx.Caster.AbilityC.Abilities.TryGetValue(ctx.Ability.Name, out var ability))
            return CommandResult.Fail(this, CommandFailureType.AbilityNotFound, $"Ability '{ctx.Ability.Name}' not found.");

        int index = payload.AccumulatedTargets.LastIndexOf(targetedTile);
        if (index == -1)
            return CommandResult.Fail(this, CommandFailureType.InvalidArgument, $"Target '{targetedTile}' not present in payload.");

        var previewAccumulatedTargets = payload.AccumulatedTargets.ToList();
        previewAccumulatedTargets.RemoveAt(index);
        if (!ctx.Caster.AbilityC.ValidateCast(
                ctx.Ability.Name,
                ctx,
                previewAccumulatedTargets,
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
        
        payload.SetValuesTo(resimulatedPayload);
        Log.Here().Info(
            "Removed target {TargetTile} for ability '{AbilityName}'",
            targetedTile,
            ctx.Ability.Name,
            ctx.Caster.UnitName,
            ctx.Caster.UnitId);
        
        context.AbilityVisualizer.ShowEffectVisualization(ctx, resimulatedPayload, ability.Effects);
        context.AbilityVisualizer.ShowReachVisualization(ctx, resimulatedPayload, ability);

        return CommandResult.Ok(this);
        
    }
}