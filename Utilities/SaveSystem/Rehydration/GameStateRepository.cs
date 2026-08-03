#nullable enable
using System.IO;
using AKidsDream.Common.Logging;
using AKidsDream.Managers.SaveSystem.Resources;
using Serilog;

namespace AKidsDream.Managers.SaveSystems.Rehydration;

/// <summary>
/// Sole responsibility: read and write <see cref="GameStateData"/> resources on disk.
/// Distinguishes "no save file yet" (fine, start fresh) from "save file exists but
/// couldn't be parsed" (corrupted — should not be silently swallowed into a blank state).
/// </summary>
public class GameStateRepository
{
    private static readonly ILogger Log = GameLogger.For(typeof(GameStateRepository));
    
    public static GameStateData Load(string stateFileName)
    {
        var path = Path.Combine(Global.SavePath, stateFileName);
 
        if (!File.Exists(path))
        {
            Log.ForContext("StateFileName", stateFileName)
                .Here()
                .Warn("Save file '{StateFileName}' not found at '{Path}'; starting a new game state",
                    stateFileName, path);
            return new GameStateData();
        }
        
        var state = ResourceIO.Load<GameStateData>(path);

        if (state == null)
        {
            Log.ForContext("StateFileName", stateFileName)
                .Here()
                .Error("Save file '{StateFileName}' exists at '{Path}' but failed to load", stateFileName, path);
 
            throw new InvalidDataException(
                $"Save file '{stateFileName}' could not be parsed. The file is likely corrupted.");
        }

        state.PlayerData ??= [];
        state.TeamData ??= [];
        state.UnitStateResources ??= [];
        
        return state;
    }

    public static void Save(GameStateData state, string stateFileName)
    {
        var path = Path.Combine(Global.SavePath, stateFileName);

        if (Path.Exists(path))
        {
            Log.ForContext("StateFileName", stateFileName)
                .Here()
                .Warn("Save file '{StateFileName}' already exists at '{Path}'; overwriting",
                    stateFileName, path);
        }
        
        ResourceIO.Save(state, path);
 
        Log.ForContext("StateFileName", stateFileName)
            .ForContext("UnitCount", state.UnitStateResources.Count)
            .Here()
            .Info("Saved game state to '{StateFileName}' with {UnitCount} units",
                stateFileName, state.UnitStateResources.Count);
    }
}