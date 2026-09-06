#nullable enable
using AKidsDream.Common.Errors;
using AKidsDream.Common.Logging;
using AKidsDream.Common.Results;
using AKidsDream.GameBoard;
using Serilog;

namespace AKidsDream.Commands;

public class EndTurnCommand(PlayerId playerId) : IGameCommand
{
    public Result<GameError> Execute(GameContext context)
    {
        var activePlayer = context.GameLoopManager.GetActivePlayer();
        if (!context.GameLoopManager.EndPlayerTurn(playerId))
        {
            return Result<GameError>.Fail(new ValidationError.NotPlayerTurn(activePlayer.PlayerId, playerId, "End turn"));
        }

        Log.ForContext<EndTurnCommand>().Here().Info(
            "Successfully ended {PlayerId}'s turn",
            playerId
        );
        return Result<GameError>.Ok();
    }
}
