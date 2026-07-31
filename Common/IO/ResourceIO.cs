using System;
using System.IO;
using Godot;

namespace AKidsDream.Managers.SaveSystems;

public static class ResourceIO
{
    public static void EnsureDirectoryExists(string path)
    {
        if (path == null) throw new ArgumentNullException(nameof(path));
        string dirPath = path.GetBaseDir();
        
        if (!DirAccess.DirExistsAbsolute(dirPath))
        {
            DirAccess.MakeDirAbsolute(dirPath);
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
            GD.PrintErr("Path is not set, cannot save resource");
            return null;
        }
        
        EnsureDirectoryExists(path);
        // try catch
        try
        {
            T resource = ResourceLoader.Load<T>(path);
            if (resource == null)
            {
                GD.Print($"Failed to load resource from {path}");
                return null;
            }
            GD.Print($"Resource loaded from {path}");
            return resource;
        }
        catch (InvalidCastException e)
        {
            GD.PrintErr($"Resource at {path} is not of type {typeof(T)}: {e.Message}");
            return null;
        }
    }

    public static Error Save(Resource resource, string path)
    {
        path = SetFileExtension(path, ".tres");

        if (string.IsNullOrEmpty(path))
        {
            GD.PrintErr("Path is not set, cannot save resource");
            return Error.InvalidParameter;
        }
        
        EnsureDirectoryExists(path);

        Error result = ResourceSaver.Save(resource, path);
        if (result == Error.Ok)
            GD.Print($"Resource saved to {path}");
        else
            GD.PrintErr($"Failed to save resource: {result}");
        
        return result;
    }
    
    public static string? SetFileExtension(string path, string extension)
    {
        if (string.IsNullOrEmpty(path)) return null;
        
        path = path.TrimEnd('/').TrimEnd('\\');
        path = Path.ChangeExtension(path, extension);
        GD.Print($"File extension set to {extension} for {path}");
        
        return path;
    }
}