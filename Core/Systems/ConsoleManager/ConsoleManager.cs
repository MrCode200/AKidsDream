#nullable enable
using System;
using AKidsDream.Utilities.Logging;
using AKidsDream.Commands;
using AKidsDream.Managers.SaveSystems;
using Godot;
using Serilog;

namespace AKidsDream.Util.Identifiers.Commands;

public interface IConsoleCommand
{
	void Init(GameContext context);
	void Exit();
}

[GlobalClass]
[Icon("res://Core/Systems/ConsoleManager/terminal.svg")]
public partial class ConsoleManager : Node
{
	private static readonly ILogger Log = GameLogger.For(typeof(ConsoleManager));
	private GameContext? _context;

	public void Init(GameContext context)
	{
		_context = context;
		RegisterCommands();
		Log.Here().Debug("ConsoleManager initialized with GameContext");
	}

	public override void _ExitTree()
	{
		foreach (var (key, cmdDef) in Global.ConsoleCommandRegistry.Item)
		{
			try {
				cmdDef.Command.Exit();
			} catch (Exception e) {
				Log.Here().Err(e, "Failed to initialize console command {NamespacedKey}", key);
			}
		}
	}
	
	public void RegisterCommands()
	{
		foreach (var (key, cmdDef) in Global.ConsoleCommandRegistry.Item)
		{
			try {
				cmdDef.Command.Init(_context!);
			} catch (Exception e) {
				Log.Here().Err(e, "Failed to initialize console command {NamespacedKey}", key);
			}
		}
	}
}
