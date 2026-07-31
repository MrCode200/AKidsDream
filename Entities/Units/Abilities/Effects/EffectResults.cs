using AKidsDream.Units.Resources;
using Godot;
using Godot.Collections;

namespace AKidsDream.Abilities.Effects;

public partial class EffectResult : RefCounted { }
public partial class DamageResult : EffectResult { public Unit Target; public Vector2I Tile; public int Amount; }
public partial class MoveResult : EffectResult { public Unit Source; public Vector2I From; public Vector2I To; }
public partial class CompositeResult : EffectResult { public Array<EffectResult> Results; }

// -- ERRORS --
public partial class ErrorResult : EffectResult { public Unit Source; public EffectData Effect; public string Error; }
public partial class InvalidTargetCountErrorResult : ErrorResult { public int Actual; }