#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AKidsDream.Common.Logging;
using AKidsDream.Commands;
using Godot;
using Serilog;

namespace AKidsDream.Util.Identifiers.Commands;

[AttributeUsage(AttributeTargets.Class)]
public class ConsoleCommandAttribute : Attribute { }

public interface IConsoleCommand
{
	void SetContext(GameContext context);
}

[GlobalClass]
[Icon("res://Core/Systems/ConsoleManager/terminal.svg")]
public partial class ConsoleManager : Node
{
	private static readonly ILogger Log = GameLogger.For(typeof(ConsoleManager));
	private List<Type> _registeredCommands = null!;
	private GameContext? _context;

	public void Init(GameContext context)
	{
		_context = context;
	}
	
	public override void _Ready()
	{
		_registeredCommands = _findCommandTypes();
		AddCommandsAsChild();
	}

	private static List<Type> _findCommandTypes()
	{
		var assembly = Assembly.GetExecutingAssembly();

		var foundCommands = assembly.GetTypes()
			.Where(t => t.IsDefined(typeof(ConsoleCommandAttribute), false))
			.Where(t => !t.IsAbstract)
			.ToList();
		
		Log.Here().Debug("Found {CommandCount} commands", foundCommands.Count);
		return foundCommands;
	}
	// -- Private Methods --

	private void AddCommandsAsChild()
	{
		foreach (var commandType in _registeredCommands)
		{
			var commandInstance = Activator.CreateInstance(commandType)!;
			if (commandInstance is not IConsoleCommand command)
			{
				Log.Here().Err("Failed to create command '{CommandType}' as IConsoleCommand", commandType);
				continue;
			}
			
			command.SetContext(_context!);
			if (command is Node node)
			{
				AddChild(node);
				node.Name = commandType.Name;
				Log.Here().Debug("Added command '{CommandType}' as child", commandType);
			}
			else
				Log.Here().Err("Failed to add command '{CommandType}' as child. (Command is not a Node)", commandType);
		}
	}
}
