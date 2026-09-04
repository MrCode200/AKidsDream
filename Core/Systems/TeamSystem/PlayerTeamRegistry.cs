using System.Diagnostics.CodeAnalysis;
using AKidsDream.Common.Logging;
using AKidsDream.Core.Teams;
using AKidsDream.Util.Identifiers;
using AKidsDream.Core.Managers;
using Godot.Collections;
using Serilog;

/// <summary>
/// Registry for player and team data. Owned by GameManager.
/// This is the only place PlayerData/TeamData collections are owned.
/// </summary>
public class PlayerTeamRegistry(TeamRelationResolver teamRelationResolver)
{
    private static readonly ILogger Log = GameLogger.For(typeof(PlayerTeamRegistry));
    
    private readonly System.Collections.Generic.Dictionary<PlayerId, PlayerData> _players = new();
    private readonly System.Collections.Generic.Dictionary<TeamId, TeamData> _teams = new();
    
    // -- GETTER/SETTERS --
    public void RegisterPlayer(PlayerData player)
    {
        if (_players.ContainsKey(player.PlayerId)) 
            Log.Here().Fatal("Player {PlayerId} tried to register twice, replacing old PlayerId", player.PlayerId);
        _players[player.PlayerId] = player;
    }
    
    public void RegisterTeam(TeamData team)
    {
        if (_teams.ContainsKey(team.TeamId)) 
            Log.Here().Fatal("Team {TeamId} tried to register twice, replacing old TeamId", team.TeamId);
        _teams[team.TeamId] = team;
    }
    
    public bool TryGetPlayer(PlayerId playerId, [NotNullWhen(true)] out PlayerData? player)
    {
        player = null;
        if (!_players.TryGetValue(playerId, out player)) return false;
        return true;
    }
    
    public bool TryGetTeam(TeamId teamId, [NotNullWhen(true)] out TeamData? team)
    {
        team = null;
        if (!_teams.TryGetValue(teamId, out team)) return false;
        return true;
    }
    
    /// <summary>
    /// Returns true if the player is on a team.
    /// </summary>
    /// <param name="playerId">The Players whose team you want to get</param>
    /// <param name="teamId">Returns default <see cref="TeamId"/> if <see cref="PlayerId"/> not registered</param>
    /// <returns></returns>
    public bool TryGetPlayersTeamId(PlayerId playerId, [NotNullWhen(true)] out TeamId? teamId)
    {
        teamId = null;
        if (!_players.TryGetValue(playerId, out var player)) return false;
        
        teamId = player.TeamId;
        return true;
    }
    
    public PlayerData[] GetAllPlayers()
    {
        return [.. _players.Values];
    }
    
    public TeamData[] GetAllTeams()
    {
        return [.. _teams.Values];
    }
    
    // -- HELPERS --

    /// <summary>
    /// Returns true if the target player is hostile to the local player.
    /// </summary>
    /// <param name="callerPlayerId">The player to check hostility against</param>
    /// <param name="otherPlayerId">The local player's ID</param>
    /// <param name="teamRelationResolver">The team relation resolver to check team relations</param>
    /// <returns>True if hostile, false if friendly or if relation cannot be determined</returns>
    public bool IsHostileToPlayer(PlayerId callerPlayerId, PlayerId otherPlayerId)
    {
        if (!TryGetPlayersTeamId(callerPlayerId, out var callerTeamId))
        {
            Log.Here().Warn("Caller player {PlayerId} not registered, assuming hostile", callerPlayerId);
            return true;
        }

        if (!TryGetPlayersTeamId(otherPlayerId, out var otherTeamId))
        {
            Log.Here().Warn("Other player {PlayerId} not registered, assuming hostile", otherPlayerId);
            return true;
        }

        return !teamRelationResolver.IsFriendly(otherTeamId.Value, callerTeamId.Value);
    }

}