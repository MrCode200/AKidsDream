GD Console: https://www.youtube.com/watch?v=M_ymfQtZad4

CallableStateMachine: https://gist.github.com/firebelley/96f2f82e3feaa2756fe647d8b9843174

For entities i would like the baseNode as well as the derived Nodes when logging to be distinctable via
log() -> to set id and id Name (ex: Unit{ 1 }) to be printed
should i use libaries if not how to implement

If log order becomes messy add canonical logging

Before Logging:

Level	Usage
Verbose	Verbose is the noisiest level, rarely (if ever) enabled for a production app.
Debug	Debug is used for internal system events that are not necessarily observable from the outside, but useful when determining how something happened.
Information	Information events describe things happening in the system that correspond to its responsibilities and functions. Generally these are the observable actions the system can perform.
Warning	When service is degraded, endangered, or may be behaving outside of its expected parameters, Warning level events are used.
Error	When functionality is unavailable or expectations broken, an Error event is used.
Fatal	The most critical level, Fatal events demand immediate attention.

How to log:
1. Prefer structured logging with named properties instead of plain text.
   Example: Log.Information("LevelStarted {LevelName} {PlayerId}", levelName, playerId);
    Always include relevant parameters so I can filter logs later.
2. When catching exceptions (log at Error or Fatal and include exception details).
    When branching into unusual or fallback behavior (log at Warning).
   Add logs at key points of the game flow, not everywhere:
3. Each log entry should provide useful context:
   Who: player ID, entity ID, or network peer.
What: action or operation name (“SaveGame”, “LoadScene”, “ApplyDamage”).
Where: scene name, node path, or system/component name.
Why/Result: success/failure, error message, key parameters.
4. if not critical try to Avoid expensive string operations inside tight loops.
Don’t log every frame or every physics tick unless I explicitly ask for verbose debugging. 
5. Avoid log spam and duplication:
   Don’t log the same error on every layer of the call stack; log it where it is handled.

Prefer one clear Error or Fatal log with full context over many partial messages.
Identify important methods and systems (scene management, save/load, combat, inventory, UI flows) and insert appropriate Serilog calls with the right log level.
Improve existing log messages to be structured, clear, and context‑rich (who/what/where/why).
Remove or downgrade noisy logs that don’t help with debugging or monitoring. (includes GD.Print PushError etc.)
When you add logging around exception handling, show how to include the exception (Log.Error(ex, "...") or Log.Fatal(ex, "...")) and what contextual properties to include.
When you suggest changes, explain briefly why each log statement is useful and why you chose that log level.

