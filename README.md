https://www.youtube.com/watch?v=M_ymfQtZad4

NOTE: 
	we have TileData in BoardState but it is not Used and empty 
	only placed TileData is used is in Board.cs

You already have the answer to your biggest "THINK" question in your own codebase — **Godot C# resources absolutely can hold executable logic, and you're already doing it**. `AccessFieldPattern` is an abstract `Resource` with a polymorphic `GetTiles(...)` method, and `AdjacentPattern` overrides it. [2-cite-0](#2-cite-0) [2-cite-1](#2-cite-1)  So "Resources aren't meant for logic" is a convention, not a hard rule — and your project already broke it deliberately.

## Where you are today

`MoveComponent` and `AttackComponent` are indeed near-duplicates: both hold `[Export] Unit Unit`, both hold an `[Export] AccessFieldPattern` (your "Area Pattern" already exists), both call `Pattern.GetTiles(Unit.TileLocation, Board.Instance, Unit.Stats)`, then run their `ValidX`/`ValidateX`/`Do` trio. [2-cite-2](#2-cite-2) [2-cite-3](#2-cite-3)  The only real difference is the terminal action: `MoveComponent` writes `Unit.TileLocation`; `AttackComponent` calls `target.HealthC.Damage(...)`. [2-cite-4](#2-cite-4) [2-cite-5](#2-cite-5)  That terminal action is exactly your proposed "Effect Pattern / What to Do".

Cost already has a home too: `ActionComponent` tracks `MaxMoveActions`/`MoveActions` and decrements on the local `MoveComponent.UnitMoved` signal. [2-cite-6](#2-cite-6) 

## Recommended design (grounded in what you have)

Split your `MResource` into **data + polymorphic sub-resources**, mirroring the `AccessFieldPattern` pattern you already trust. Don't put all logic in one giant resource; compose it:

```
Ability : Resource          # your "MResource"
├─ AccessFieldPattern AreaPattern   # REUSE existing — reach/selectable tiles
├─ AccessFieldPattern EffectPattern # where effects land relative to selected tile (nullable)
├─ AbilityVisualization Visualization  # nullable → fall back to AreaPattern/EffectPattern
├─ Effect Effect            # abstract Resource w/ Apply(context) — the "What to do"
├─ StringName AnimationName
├─ int Cost
└─ StringName CostPool      # "MoveAction", etc.
```

- `AreaPattern`/`EffectPattern`: keep them as `AccessFieldPattern` — no new type needed, it already returns tiles. `PatternMode` might need to grow beyond `Move`/`Attack` (or be replaced by a filter delegate). [2-cite-7](#2-cite-7) 
- `Effect`: a new abstract `Effect : Resource` with `public abstract void Apply(AbilityContext ctx)`. Subclasses `DamageEffect`, `HealEffect`, `MoveEffect`, `DamageAndJumpEffect` — each with their own `[Export] int Amount` etc. This is the same inheritance shape as `AdjacentPattern`.

## On signals + metadata (your "Pass Metadata" question)

Don't pass loosely-typed metadata through `EventBus`. Your `EmitSignal` calls take `Variant` args, so a "predetermined inputs based on type" scheme becomes stringly-typed and fragile. [2-cite-8](#2-cite-8)  Instead, build a typed **context object** that carries everything:

```csharp
public struct AbilityContext {
    public Unit Caster;
    public Vector2I SelectedTile;
    public Vector2I[] EffectTiles;
    public Board Board;
}
```

`Effect.Apply(ctx)` reads what it needs. If you still want cross-system notification via `EventBus`, keep firing the existing coarse signals (`UnitMoved`, `UnitKilled`) from inside the concrete effects — that preserves your current `Board`/`InputHandler` subscribers without inventing a metadata protocol. [2-cite-9](#2-cite-9) 

## The new handler/component

Create one `AbilityComponent` that replaces both `MoveComponent` and `AttackComponent`:

```csharp
[GlobalClass]
public partial class AbilityComponent : Node {
    [Export] public Unit Unit;
    [Export] public Godot.Collections.Array<Ability> Abilities;

    public Vector2I[] ValidTiles(Ability a) =>
        a.AreaPattern.GetTiles(Unit.TileLocation, Board.Instance, Unit.Stats);

    public bool Use(Ability a, Vector2I tile) { /* validate → pay cost → Apply effect → emit */ }
}
```

Cost management: let `AbilityComponent` ask `ActionComponent` whether `CostPool` has enough points and decrement it, generalizing the current hard-coded `MoveActions`. `ActionComponent` already owns that responsibility, so extend it into a keyed pool (`Dictionary<StringName,int>`) rather than a single `MoveActions` field. [2-cite-10](#2-cite-10) 

## Direct answers to your THINK list

- **Attachable scripts / custom logic in resources?** Yes — `[GlobalClass] partial class X : Resource` with virtual/abstract methods works and is already used. Custom effect logic in resource subclasses is fine. [2-cite-11](#2-cite-11) 
- **"Resources aren't meant for logic"?** True as a *purist* guideline (resources = serialized data). The pragmatic middle ground you should use: put **behavior selection** in polymorphic resource subclasses (Strategy pattern), keep **tunable numbers** (`Amount`, `Duration`, `Cost`) as `[Export]` data on those subclasses. That's exactly your instinct about "a resource for damage amount and maybe duration" — that's the right call.
- **CustomVisualization fallback:** model it as a nullable `[Export]` on `Ability`; when null, `Visualizer` uses `AreaPattern` tiles, then `EffectPattern` tiles on hover — this fits how `Visualizer.ShowUnitValidMoves` already pulls tiles from the component. [2-cite-12](#2-cite-12) 

One caution: your `_injectReferenceAndAssignComponents` manually re-wires components because `[Export]` node refs don't survive `_Ready`. If you collapse Move/Attack into `AbilityComponent`, update that injection block and the `ActionComponent` wiring accordingly. [2-cite-13](#2-cite-13) 

Want me to turn this into an implementation plan for the `Ability`/`Effect` resources + `AbilityComponent`, or keep iterating on the design first?
