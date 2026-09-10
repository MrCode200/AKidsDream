#nullable enable
using System.Threading.Tasks;
using AKidsDream.Common;
using AKidsDream.Common.Components.TweenComponent.Resources;
using AKidsDream.Common.Errors;
using AKidsDream.Common.Logging;
using AKidsDream.Common.Results;
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
    public async Task<Result<GameError>> ExecuteAsync(GameContext context)
    {
        var castResult = await caster.AbilityC.CastAsync(abilityName, abilityContext, payload.AccumulatedTargets);

        if (castResult.IsFailure)
            return castResult.DropValue();

        context.AbilityVisualizer.ClearTilemaps();

        Log.ForContext<CastAbilityBaseCommand>().Here().Info(
            "Casted ability '{AbilityName}' for unit '{UnitName}' (id: {UnitId}) at {TargetCount} target(s)",
            abilityName,
            caster.UnitName,
            caster.UnitId,
            payload.ProcessingTiles.Count);

        return Result<GameError>.Ok();
    }
}