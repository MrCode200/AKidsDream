#nullable enable
using System.Threading.Tasks;
using AKidsDream.Utilities;
using TweenComp.Resources.Blueprint;
using AKidsDream.Utilities.Errors;
using AKidsDream.Utilities.Logging;
using AKidsDream.Utilities.Results;
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
    private static readonly ILogger Log = GameLogger.For<CastAbilityBaseCommand>();

    public async Task<Result<GameError>> ExecuteAsync(GameContext context)
    {
        var castResult = await caster.AbilityComp.CastAsync(abilityName, abilityContext, payload.AccumulatedTargets);

        if (castResult.IsFailure)
            return castResult.DropValue();

        context.AbilityVisualizer.ClearTilemaps();

        Log.ForContext("UnitIdTag", caster.UnitId)
            .ForContext("UnitNameTag", caster.UnitType)
            .ForContext("AbilityNameTag", abilityName)
            .Here().Info(
            "Casted ability at {TargetCount} target(s)",
            payload.ProcessingTiles.Count);

        return Result<GameError>.Ok();
    }
}