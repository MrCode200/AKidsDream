using System.Collections.Generic;
using AKidsDream.Abilities;
using AKidsDream.Common.Logging;
using AKidsDream.Units.Resources;
using Godot;
using Serilog;

namespace AKidsDream.Commands;

// CHECK: maybe not needed as command, as this data won't help for cast ability as both need inputs to be serialized
// CHECK: maybe needed to check visualization, like SelectAbilityCommand...
public sealed class AddAbilityTargetCommand(
    Unit caster,
    StringName abilityName,
    Vector2I targetTile,
    List<Vector2I> selectedTargets
) : IGameCommand
{
    public const string InvalidTargetReason = "Target exceeds max duplicate targets allowed.";
    
    public CommandResult Execute(GameContext context)
    {
        if (caster is null)
            return CommandResult.Fail(this, CommandFailureType.NullArgument, "No caster was provided.");
        
        if (abilityName is null)
            return CommandResult.Fail(this, CommandFailureType.NullArgument, "No ability name was provided.");
        
        var ability = caster.AbilityC.GetAbility(abilityName);
        if (!ability!.Effect.HasValidTargetCount([.. selectedTargets, targetTile]))
            return CommandResult.Fail(
                this,
                CommandFailureType.MaxDuplicateTargetsExceeded,
                $"Target exceeds max duplicate targets of {ability.Effect.MaxDuplicateTargets} allowed."
            );

        Log.ForContext<AddAbilityTargetCommand>().Here().Info(
            "Added target {TargetTile} for ability '{AbilityName}' for unit '{UnitName}' (id: {UnitId})",
            targetTile,
            abilityName,
            caster.UnitName,
            caster.UnitId);
        
        selectedTargets.Add(targetTile);
        
        context.AbilityVisualizer.ShowEffectVisualization(
            caster,
            [.. selectedTargets],
            ability.Effect
        );
        
        return CommandResult.Ok(this);
    }
}