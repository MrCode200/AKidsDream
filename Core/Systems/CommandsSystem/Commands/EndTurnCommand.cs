#nullable enable
using AKidsDream.Common.Errors;
using AKidsDream.Common.Logging;
using AKidsDream.GameBoard;
using Serilog;

namespace AKidsDream.Commands;

public class EndTurnCommand(PlayerId playerId) : IGameCommand
{
    public CommandResult Execute(GameContext context)
    {
        var activePlayer = context.GameLoopManager.GetActivePlayer();
        if (!context.GameLoopManager.EndPlayerTurn(playerId))
        {
            return CommandResult.Fail(this, new CommandError.NotPlayerTurn(activePlayer.PlayerId, playerId));
        }

        Log.ForContext<EndTurnCommand>().Here().Info(
            "Successfully ended {PlayerId}'s turn",
            playerId
        );
        return CommandResult.Ok(this);
    }
}
