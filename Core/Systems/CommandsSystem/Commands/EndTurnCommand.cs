#nullable enable
using AKidsDream.Utilities.Errors;
using AKidsDream.Utilities.Logging;
using AKidsDream.Utilities.Results;
using Serilog;

namespace AKidsDream.Commands;

public class EndTurnCommand(PlayerId playerId) : IGameCommand
{
    private static readonly ILogger Log = GameLogger.For<EndTurnCommand>();

    public Result<GameError> Execute(GameContext context)
    {
        var activePlayer = context.GameLoopManager.GetActivePlayer();
        if (!context.GameLoopManager.EndPlayerTurn(playerId))
        {
            return Result<GameError>.Fail(new ValidationError.NotPlayerTurn(activePlayer.PlayerId, playerId, "End turn"));
        }

        Log.ForContext("PlayerIdTag", playerId)
            .Here().Info("Successfully ended turn");
        return Result<GameError>.Ok();
    }
}
