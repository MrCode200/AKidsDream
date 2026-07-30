using System.Collections.Generic;
using AKidsDream.Abilities;
using AKidsDream.Units;
using Godot;

namespace AKidsDream.Commands;

public sealed class AddAbilityTargetCommand(
    Unit caster,
    StringName abilityName,
    Vector2I targetTile,
    List<Vector2I> selectedTargets
) : IGameCommand
{
    public CommandResult Execute(GameContext context)
    {
        if (caster is null)
            return CommandResult.Fail(this, "No caster was provided.");
        
        if (abilityName is null)
            return CommandResult.Fail(this, "No ability name was provided.");
        
        AbilityData ability = caster.AbilityC.GetAbility(abilityName);
        if (!ability!.Effect.AllowDuplicateTiles &&
            selectedTargets.Contains(targetTile))
            return CommandResult.Fail(this, "Target already targeted. Ability doesn't support duplicate targets.");
        
        selectedTargets.Add(targetTile);
        
        context.AbilityVisualizer.ShowEffectVisualization(
            caster,
            [.. selectedTargets],
            ability.Effect
        );
        
        return CommandResult.Ok(this);
    }
}