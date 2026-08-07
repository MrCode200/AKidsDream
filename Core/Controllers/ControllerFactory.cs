#nullable enable
using System;
using AKidsDream.Commands;
using AKidsDream.Controllers;
using AKidsDream.Core.Teams;
using AKidsDream.GameBoard;
using AKidsDream.Managers;
using AKidsDream.StateMachines;
using Godot;

namespace AKidsDream.Core.Controllers;

public enum ControllerType
{
	PlayerInteractionController,
}

public interface IPlayerController
{
	public void StartTurn();
	public void EndTurn();
}

public sealed class PlayerControllerContext(
	Board board,
	AbilityVisualizer abilityVisualizer,
	CommandExecutor commandExecutor
)
{
	public Board Board => board;
	public AbilityVisualizer AbilityVisualizer => abilityVisualizer;
	public CommandExecutor CommandExecutor => commandExecutor;
};

public partial class ControllerFactory : Node
{
	[Export] public Board Board = null!;
	[Export] public AbilityVisualizer AbilityVisualizer = null!;
	[Export] public CommandExecutor CommandExecutor = null!;
	private PlayerControllerContext? _context;

	private void SetContext()
	{
		_context = new PlayerControllerContext(Board, AbilityVisualizer, CommandExecutor);
	}

	// -- FACTORY --
	/// <summary>
	/// Creates a new controller instance of the specified type and adds it to the node tree as its child.
	/// </summary>
	/// <param name="playerData">The Player to whom the controller belongs</param>
	/// <returns></returns>
	/// <exception cref="ArgumentException">If no controller with the specified <see cref="ControllerType"/> is found</exception>
	public IPlayerController CreateController(PlayerData playerData)
	{
		if (_context == null) SetContext();
		
		IPlayerController? controller = null;
		switch (playerData.ControllerType)
		{
			case ControllerType.PlayerInteractionController:
				controller = new PlayerInteractionController(_context!, playerData);
				break;
			default:
				throw new ArgumentException($"Unknown controller type: {playerData.ControllerType}");
		}

		if (controller is Node node)
			AddChild(node);
		return controller;
	}
}
