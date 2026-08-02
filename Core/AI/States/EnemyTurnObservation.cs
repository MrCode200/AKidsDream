#nullable enable
using System;
using AKidsDream.StateMachines;

namespace AKidsDream.Managers.AI;

/*
 * Enemy Turn Observation:
 * 1. Clicking a unit; shows their stats (Optionally show the ability list in disabled form)
 * 2. Hovering over a unit; shows information in short form //TODO: add this also to other States (As this is always active how to code for all states...?)
 * 3.  
 */
/*
 * Option A: Tactical forecast
Let the player click an enemy unit and see:
likely target
attack range
movement range
current intent
 */

// Rename to ReadOnlyInspection, if needed for more general use case
public class EnemyTurnObservation : IState
{
    public Action<IState, string, bool> ChangeState { get; set; } = null!;
    
    
}