using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AKidsDream.Common.Logging;
using Godot;
using Serilog;

namespace AKidsDream.Core.Controllers.Commands;

[AttributeUsage(AttributeTargets.Class)]
public class ConsoleCommandAttribute : Attribute { }

public partial class ConsoleManager : Node
{
    private static readonly ILogger Log = GameLogger.For(typeof(ConsoleManager));
    private List<Type> _registeredCommands;
    
    public override void _Ready()
    {
        _registeredCommands = _findCommandTypes();
        AddCommandsAsChild();
    }

    private static List<Type> _findCommandTypes()
    {
        var assembly = Assembly.GetExecutingAssembly();

        return assembly.GetTypes()
            .Where(t => t.IsDefined(typeof(ConsoleCommandAttribute), false))
            .Where(t => !t.IsAbstract)
            .ToList();
    }
    // -- Private Methods --

    private void AddCommandsAsChild()
    {
        foreach (var commandType in _registeredCommands)
        {
            var command = Activator.CreateInstance(commandType)!;
            if (command is Node node)
            {
                AddChild(node);
                node.Name = commandType.Name;
                Log.Here().Debug("Added command '{CommandType}' as child", commandType);
            }
            else
                Log.Here().Error("Failed to add command '{CommandType}' as child. (Command is not a Node)", commandType);
        }
    }
}