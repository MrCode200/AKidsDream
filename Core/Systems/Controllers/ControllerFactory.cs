#nullable enable
using System;
using AKidsDream.Commands;
using AKidsDream.Controllers;
using AKidsDream.Core.Content;
using AKidsDream.Core.Teams;
using AKidsDream.Utilities.Logging;
using AKidsDream.Managers.SaveSystems;
using Godot;
using Serilog;

namespace AKidsDream.Util.Identifiers;

public interface IController
{
	public bool UseEndTurnButton { get; set; }
	public void StartTurn();
	public void EndTurn();
}

[Icon("res://Core/Systems/Controllers/controller-icon.png")]
public partial class ControllerFactory : Node
{
	private GameContext? _context;
	private readonly ILogger _log = GameLogger.For<ControllerFactory>();

	public void Init(GameContext context)
	{
		_context = context;
		_log.Here().Debug("ControllerFactory initialized with GameContext");
	}

	// -- FACTORY --
	/// <summary>
	/// Creates a new controller instance of the specified type and adds it to the node tree as its child.
	/// </summary>
	/// <param name="playerData">The Player to whom the controller belongs</param>
	/// <returns></returns>
	/// <exception cref="ArgumentException">If no controller with the specified string is found in the registry</exception>
	public IController CreateController(PlayerData playerData)
	{
		if (_context == null)
			throw new InvalidOperationException("ControllerFactory not initialized. Call Initialize() first.");
		
		_log.Here().Debug("Creating controller of type {ControllerType} for player {PlayerName} (id: {PlayerId})",
			playerData.ControllerKey, playerData.Name, playerData.PlayerId);
		
		var controllerDef = Global.ControllerRegistry.Get(playerData.ControllerKey);
		IController controller = controllerDef.Factory(_context, playerData);

		if (controller is Node node)
			AddChild(node);
		
		_log.Here().Debug("Successfully created and added {ControllerType} controller for player {PlayerName}",
			playerData.ControllerKey, playerData.Name);
		
		return controller;
	}
}
