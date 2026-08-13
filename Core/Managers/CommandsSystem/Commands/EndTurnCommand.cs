using AKidsDream.Common.Logging;
using Serilog;

namespace AKidsDream.Commands;

public class EndTurnBaseCommand(PlayerId playerId) : IGameBaseCommand
{
    public CommandResult Execute(GameContext context)
    {
        if (!context.GameLoopManager.EndPlayerTurn(playerId))
        {
            return CommandResult.Fail(this, CommandFailureType.NotPlayerTurn, $"Not {playerId}'s turn");
        }
        
        Log.ForContext<EndTurnBaseCommand>().Here().Info(
            "Successfully ended {PlayerId}'s turn",
            playerId
        );
        return CommandResult.Ok(this);
    }
}