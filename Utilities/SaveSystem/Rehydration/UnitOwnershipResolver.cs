using System.Diagnostics.CodeAnalysis;
using AKidsDream.Common.Logging;
using AKidsDream.Core.Managers;
using AKidsDream.Managers;
using AKidsDream.Util.Identifiers;
using AKidsDream.Common;
using AKidsDream.Core.Teams;
using Serilog;

namespace AKidsDream.Managers.SaveSystems.Rehydration;

public static class UnitOwnershipResolver
{
    private static readonly ILogger Log = GameLogger.For(typeof(UnitOwnershipResolver));

    public readonly record struct Ownership(PlayerData PlayerData, TeamId TeamId);

    public static bool TryResolve(UnitStateData state, PlayerTeamRegistry playerTeamRegistry,
        [NotNullWhen(true)] out Ownership? ownership)
    {
        ownership = null;

        // Checked before constructing PlayerId so a corrupted OwnerId in a save file
        // produces a clean "skip this unit" instead of an uncaught exception from PlayerId's
        // own invariant check.
        if (state.OwnerId <= 0)
        {
            Log.ForContext("NameTag", state.UnitName)
                .Here()
                .Err("Unit '{UnitName}' has invalid OwnerId {OwnerId} in save data; skipping unit",
                    state.UnitName, state.OwnerId);
            return false;
        }


        if (
            !playerTeamRegistry.TryGetPlayer(new PlayerId(state.OwnerId), out var player) ||
            player.PlayerIdInt == 0)
        {
            Log.ForContext("NameTag", state.UnitName)
                .Here()
                .Err("Unit '{UnitName}' has an unregistered or invalid owner (OwnerId: {OwnerId}); skipping unit",
                    state.UnitName, state.OwnerId);
            return false;
        }

        if (
            !playerTeamRegistry.TryGetPlayersTeamId(player.PlayerId, out var teamId) ||
            teamId?.Value == 0
        )
        {
            Log.ForContext("NameTag", state.UnitName)
                .Here()
                .Err("Unit '{UnitName}' owner (OwnerId: {OwnerId}) has no valid registered team; skipping unit",
                    state.UnitName, state.OwnerId);
            return false;
        }

        ownership = new Ownership(player, teamId.Value);
        return true;
    }
}