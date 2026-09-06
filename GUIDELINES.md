# Project Resources & Guidelines

## 📚 References

| Resource             | Description                                 | Link                                                                             |
|----------------------|---------------------------------------------|----------------------------------------------------------------------------------|
| GD Console           | Godot developer console addon tutorial      | [Watch video](https://www.youtube.com/watch?v=M_ymfQtZad4)                       |
| CallableStateMachine | Lightweight state machine gist for Godot C# | [View gist](https://gist.github.com/firebelley/96f2f82e3feaa2756fe647d8b9843174) |

## 🕵️ Error-Handling Guidelines

**Throw** when: Unexpected failure, programmer error, corrupted state, violated invariant \
**Validate + return Result.Fail** when: Expected gameplay failure (insufficient mana, invalid target, ability not found)\
**Catch** only when: Can meaningfully handle, translate, or recover

### Decision Questions
1. External/untrusted data? → Check + validate/throw
2. Public API boundary? → Check + throw
3. Expected gameplay failure? → Validate + return Result.Fail
4. System invariant (guaranteed by caller)? → Don't check
5. Corrupted state? → Throw immediately


## 📝 Changelog Guidelines

Format: `type(scope): short imperative description`

Add `!` after `(scope)` to mark breaking changes, e.g. `feat(unit-abilities)!: remove legacy movement API`

| Change type                         | Prefix                 | Example                                           |
|-------------------------------------|------------------------|---------------------------------------------------|
| New feature                         | `feat:`                | `feat(units): add overwatch ability`              |
| Bug fix                             | `fix:`                 | `fix(board): prevent units moving through walls`  |
| Internal change, no behavior change | `refactor:`            | `refactor(events): extract turn-state handler`    |
| Intentional removal                 | `refactor:` / `chore:` | `refactor(ai): remove deprecated target selector` |
| Documentation                       | `docs:`                | `docs(readme): add local setup steps`             |
| Tests                               | `test:`                | `test(combat): cover critical-hit calculation`    |
| Formatting only                     | `style:`               | `style: format unit-stat classes`                 |
| Performance                         | `perf:`                | `perf(pathfinding): cache reachable tiles`        |
| Tooling / dependencies              | `chore:`               | `chore: update Godot C# tooling`                  |
| CI pipeline                         | `ci:`                  | `ci: run tests on pull requests`                  |
| Build / export config               | `build:`               | `build: configure Windows export preset`          |

---

## 📊 Logging Guidelines

### 🎚️ Log Levels

| Level           | Usage                                                                                             |
|-----------------|---------------------------------------------------------------------------------------------------|
| **Verbose**     | Noisiest level; rarely enabled in production.                                                     |
| **Debug**       | Internal system events, not observable externally, but useful for tracing how something happened. |
| **Information** | Observable actions the system performs, tied to its core responsibilities.                        |
| **Warning**     | Service is degraded, endangered, or behaving outside expected parameters.                         |
| **Error**       | Functionality is unavailable or an expectation is broken.                                         |
| **Fatal**       | Most critical level; demands immediate attention.                                                 |


### 🧩 Project implemented Logging

For distinguishable logs across base and derived `Node` types:

1. **Initialize logger** using the [GameLogger](cci:2://file:///C:/Users/Navid/Documents/a-kids-dream/AKidsDream/Common/Logging/GameLogger.cs:53:0-168:1) factory:
   ```csharp
   private ILogger _log = GameLogger.For<Type>();
   ```

2. **Enrich with entity context** during initialization (e.g., in [Init()](cci:1://file:///C:/Users/Navid/Documents/a-kids-dream/AKidsDream/Entities/Units/BaseUnit/Unit.cs:55:4-108:5)):
   ```csharp
   _log = _log.ForContext("UnitId", UnitId)
       .ForContext("UnitName", UnitName)
       .ForContext("PlayerId", OwnerIdInt);
   ```

3. **Use the [.Here()](cci:1://file:///C:/Users/Navid/Documents/a-kids-dream/AKidsDream/Common/Logging/GameLogger.cs:22:4-25:47) extension** for automatic caller context (Method:Line):
   ```csharp
   _log.Here().Debug("Unit ready at {TileLocation}", TileLocation);
   ```

4. **Output format** includes:
    - Timestamp: `[HH:mm:ss]`
    - Short class name: [Unit](cci:2://file:///C:/Users/Navid/Documents/a-kids-dream/AKidsDream/Entities/Units/BaseUnit/Unit.cs:16:0-180:1)
    - Method and line: `Move:174`
    - Log level: `DBG` / `INF` / `WRN` / `ERR` / `FTL`
    - Entity context: `[UnitName:UnitId]`
    - Structured message with named properties
 
### ❓ How to Log

1. **Prefer structured logging** with named properties instead of plain text:
   ```csharp
   Log.Here().Info("LevelStarted {LevelName} {PlayerId}", levelName, playerId);
   ```
   Always include relevant parameters so logs can be filtered later.

2. **Exceptions & fallbacks:**
    - Catching an exception → log at `Error` or `Fatal` with exception details.
    - Branching into unusual/fallback behavior → log at `Warning`.
    - Add logs at key points in the game flow — not everywhere.

3. **Each log entry should answer:**
    - **Who** — player ID, entity ID, or network peer.
    - **What** — action/operation (`SaveGame`, `LoadScene`, `ApplyDamage`).
    - **Where** — scene name, node path, or system/component.
    - **Why/Result** — success/failure, error message, key parameters.

4. **Performance:**
    - Avoid expensive string operations inside tight loops (if not critical).
    - Don't log every frame/physics tick unless explicitly debugging verbosely.

5. **Avoid log spam and duplication:**
    - Don't log the same error at every layer of the call stack — log it where it's handled.

