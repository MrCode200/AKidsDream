#nullable enable
using System.Threading.Tasks;
using AKidsDream.Common;
using AKidsDream.Common.Components.TweenComponent.Resources;
using AKidsDream.Common.Errors;
using AKidsDream.Common.Logging;
using Godot;
using Serilog;

namespace AKidsDream.Commands;

public class CastAbilityBaseCommand(
    Unit caster,
    StringName abilityName,
    AbilityContext abilityContext,
    AbilityPayload payload
) : IAsyncGameBaseCommand
{
    public async Task<CommandResult> ExecuteAsync(GameContext context)
    {
        if (payload.ProcessingTiles.Count == 0)
            return CommandResult.Fail(this,
                new CommandError.InvalidArgument(nameof(payload), "No target tiles were provided."));

        var castResult = await caster.AbilityC.CastAsync(abilityName, abilityContext, payload.AccumulatedTargets);

        if (castResult.IsFailure)
            return CommandResult.Fail(this, new CommandError.CastFailed(castResult.Error));

        context.AbilityVisualizer.ClearTilemaps();

        Log.ForContext<CastAbilityBaseCommand>().Here().Info(
            "Casted ability '{AbilityName}' for unit '{UnitName}' (id: {UnitId}) at {TargetCount} target(s)",
            abilityName,
            caster.UnitName,
            caster.UnitId,
            payload.ProcessingTiles.Count);

        return CommandResult.Ok(this);
    }
}