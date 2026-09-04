using System.Collections.Generic;
using System.Linq;
using AKidsDream.Common;
using Godot;
using Godot.Collections;

namespace AKidsDream.Abilities.Effects;

public partial class EffectResult : RefCounted { public IAbilityCaster Caster; public EffectData Effect; }
public partial class DamageResult : EffectResult { public Unit Target; public Vector2I Tile; public int Amount; }
public partial class MoveResult : EffectResult { public Unit Target; public Vector2I From; public Vector2I To; }

public partial class SummonedEntityResult : EffectResult { public Unit Summoned; }

public partial class CompositeResult : EffectResult
{
    public EffectResult[] Results;

    public CompositeResult() { }
    
    public CompositeResult(EffectResult[] results, bool flatten = false, int depth = -1)
    {
        Results = results;
        if (flatten) FlattenResults(depth);
    }

    public void FlattenResults(int depth = -1)
    {
        Results = [.. Flatten(Results, depth)];
    }

    private static IEnumerable<EffectResult> Flatten(IEnumerable<EffectResult> results, int depth = -1)
    {
        foreach (var r in results)
        {
            if (r is not CompositeResult composite || depth == 0)
            {
                yield return r;
                continue;
            }
            
            var nextDepth = depth < -1 ? -1 : depth - 1;
            foreach (var child in Flatten(composite.Results, nextDepth))
            {
                yield return child;
            }
        }
    }
}

// -- ERRORS --
public partial class ErrorResult : EffectResult { public string Error; }
public partial class InvalidTargetCountErrorResult : ErrorResult { public int Actual; }