using AKidsDream.Common.Logging;
using Serilog;

namespace AKidsDream.Commands;

public class EndTurnCommand(PlayerId playerId) : IGameCommand
{
    public CommandResult Execute(GameContext context)
    {
        if (context.GameLoopManager.ActivePlayerId != playerId)
        {
            return CommandResult.Fail(this, $"Not {playerId}'s turn");
        }

        if (!context.GameLoopManager.EndPlayerTurn(playerId))
        {
            return CommandResult.Fail(this, $"Failed to end {playerId}'s turn");
        }
        
        Log.ForContext<EndTurnCommand>().Here().Info(
            "Successfully ended {PlayerId}'s turn",
            playerId
        );
        return CommandResult.Ok(this);
    }
}