using System;
using System.IO;
using Godot;

namespace AKidsDream.Globals;

public class ResourceIO
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
    
    public static T Load<T>(string path) where T : Resource
    {
        EnsureDirectoryExists(path);
        T resource = ResourceLoader.Load<T>(path);
        if (resource == null)
        {
            GD.Print($"Failed to load resource from {path}");
            return null;
        }
        GD.Print($"Resource loaded from {path}");
        return resource;
    }

    public static Error Save(Resource resource, string path)
    {
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
}