#nullable enable
using System;
using System.IO;
using Godot;
using AKidsDream.Common.Logging;
using Serilog;

namespace AKidsDream.Managers.SaveSystems;

public static class ResourceIO
{
    private static ILogger _log = GameLogger.For(typeof(ResourceIO)); 
    
    public static void EnsureDirectoryExists(string path)
    {
        if (path == null) throw new ArgumentNullException(nameof(path));
        string dirPath = path.GetBaseDir();
        
        if (!DirAccess.DirExistsAbsolute(dirPath))
        {
            DirAccess.MakeDirAbsolute(dirPath);
            _log.Here().Debug("Created directory {DirPath}", dirPath);
        }
    }
    
    /// <summary>
    /// Loads a resource from the specified path.
    /// </summary>
    /// <param name="path">The path to the save file</param>
    /// <typeparam name="T">The resource to load, returns null if no such save file was found.</typeparam>
    /// <returns>The Resource as T or null</returns>
    public static T? Load<T>(string path) where T : Resource
    {
        path = SetFileExtension(path, ".tres");
        if (string.IsNullOrEmpty(path))
        {
            _log.Here().Error("Path is not set, cannot load resource");
            return null;
        }
        
        EnsureDirectoryExists(path);
        // try catch
        try
        {
            var resource = ResourceLoader.Load<T>(path);
            if (resource == null)
            {
                _log.Here().Warn("Failed to load resource from {Path}", path);
                return null;
            }
            _log.Here().Debug("Resource loaded from {Path} {ResourceType}", path, typeof(T).Name);
            return resource;
        }
        catch (InvalidCastException e)
        {
            _log.Here().Error(e, "Resource at {Path} is not of type {ExpectedType}", path, typeof(T).Name);
            return null;
        }
    }

    public static Error Save(Resource resource, string path)
    {
        path = SetFileExtension(path, ".tres");

        if (string.IsNullOrEmpty(path))
        {
            _log.Here().Error("Path is not set, cannot save resource");
            return Error.InvalidParameter;
        }
        
        EnsureDirectoryExists(path);

        Error result = ResourceSaver.Save(resource, path);
        if (result == Error.Ok)
            _log.Here().Debug("Resource saved to {Path} {ResourceType}", path, resource.GetType().Name);
        else
            _log.Here().Error("Failed to save resource to {Path} {Error}", path, result);
        
        return result;
    }
    
    public static string? SetFileExtension(string path, string extension)
    {
        if (string.IsNullOrEmpty(path)) return null;
        if (path.EndsWith(extension)) return path;
        
        path = path.TrimEnd('/').TrimEnd('\\');
        path = Path.ChangeExtension(path, extension);
        _log.Here().Debug("File extension set to {Extension} for {Path}", extension, path);
        
        return path;
    }
}