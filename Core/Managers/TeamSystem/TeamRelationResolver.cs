using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using AKidsDream.Common.Logging;
using AKidsDream.Core.Controllers;
using Serilog;

namespace AKidsDream.Core.Teams;



/// <summary>
/// Single source of truth for how two teams feel about each other.
/// Every "can I attack/heal/select this" check should go through here -
/// this is what replaces hardcoded Player-vs-Enemy booleans.
/// </summary>
public class TeamRelationResolver
{
    private static readonly ILogger Log = GameLogger.For(typeof(TeamRelationResolver));
    private readonly Dictionary<(TeamId, TeamId), TeamRelation> _relations = new();
    /// <summary>
    /// Returns a copy of the relation dictionary.
    /// </summary>
    public Dictionary<(TeamId, TeamId), TeamRelation> Relations => new (_relations);

    public void SetRelation(TeamId team1, TeamId team2, TeamRelation relation)
    {
        if (team1.Equals(team2))
        {
            Log.Here().Warn("TeamId {TeamId} tried to set relation between itself and itself, ignoring", team1);
            return;
        };
        _relations[(team1, team2)] = relation;
        _relations[(team2, team1)] = relation; // Relations are symmetric
    }
    
    /// <summary>
    /// Returns null if no relation is set. Else returns the relation.
    /// </summary>
    /// <returns></returns>
    public bool TryGetRelation(TeamId team1, TeamId team2, [NotNullWhen(true)] out TeamRelation? relation)
    {
        relation = null;
        if (!_relations.ContainsKey((team1, team2))) return false;
        
        relation = _relations[(team1, team2)];
        return true;
    }

    /// <summary>
    /// Returns true if the two teams are friendly to each other. False otherwise (including if no relation is registered).
    /// </summary>
    public bool IsFriendly(TeamId team1, TeamId team2)
    {
        if (!TryGetRelation(team1, team2, out var relation)) return false;
        return relation == TeamRelation.Ally;
    }
}

