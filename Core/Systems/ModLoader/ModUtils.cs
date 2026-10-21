using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using AKidsDream.Commands;
using AKidsDream.Utilities.Errors;
using AKidsDream.Utilities.Logging;
using AKidsDream.Utilities.Results;
using Serilog;

namespace AKidsDream.Core.Managers.ModManager;
// Check: should the below be sealed or record?

public interface IMod
{
    void RegisterContent(IContentRegistrar registrar);
    void Init(GameContext ctx);
}

public sealed class ModManifest
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Author { get; set; }
    public string Version { get; set; }
    public int ApiVersion { get; set; }
    public string[] Dependencies { get; set; }
    public string EntryPoint { get; set; }
    public string ResourcePackPath { get; set; }
    public string Description { get; set; }
}

public static class ModValidator
{
    private static readonly ILogger Logger = GameLogger.For(typeof(ModValidator));
    private static readonly Regex IdRegex = new Regex("^[a-z][a-z0-9_]*$", RegexOptions.Compiled);

    private static readonly Regex VersionRegex =
        new Regex("^\\d+\\.\\d+\\.\\d+(-[a-zA-Z0-9-]+)?\\+?[a-zA-Z0-9-]*$", RegexOptions.Compiled);

    public static Result<ModError>[] ValidateManifest(ModManifest manifest, string modDirPath)
    {
        var results = new List<Result<ModError>>();

        var requiredFieldsResult = ValidateRequiredFields(manifest);
        results.Add(requiredFieldsResult);
        if (requiredFieldsResult.IsFailure)
            Logger.Here().Err("{Error}", requiredFieldsResult.Error);

        var idResult = ValidateId(manifest.Id);
        results.Add(idResult);
        if (idResult.IsFailure)
            Logger.Here().Err("{Error}", idResult.Error);

        var versionResult = ValidateVersion(manifest.Version);
        results.Add(versionResult);
        if (versionResult.IsFailure)
            Logger.Here().Err("{Error}", versionResult.Error);

        var entryPointResult = ValidateEntryPoint(modDirPath, manifest.EntryPoint);
        results.Add(entryPointResult);
        if (entryPointResult.IsFailure)
            Logger.Here().Err("{Error}", entryPointResult.Error);
        
        CheckFolderNameMismatch(modDirPath, manifest.Id);
        
        /*var apiVersionResult = ValidateApiVersion(manifest.ApiVersion);
        results.Add(apiVersionResult);
        if (apiVersionResult.IsFailure)
            Logger.Here().Err("{Error}", apiVersionResult.Error);    
        */ // TODO: implement API version validation
        return results.ToArray();
    }
    
    public static Result<ModError> ValidateId(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return Result.Fail<ModError>(new ModError.InvalidId(id, "ID cannot be empty"));

        if (id.Length > 50)
            return Result.Fail<ModError>(new ModError.InvalidId(id, "ID cannot exceed 50 characters"));

        if (!IdRegex.IsMatch(id))
            return Result.Fail<ModError>(new ModError.InvalidId(id,
                "ID must be snake_case: start with a lowercase letter, followed by lowercase letters, numbers, or underscores (no spaces or hyphens)"));

        return Result.Ok<ModError>();
    }

    public static Result<ModError> ValidateVersion(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
            return Result.Fail<ModError>(new ModError.InvalidVersion(version));

        if (!VersionRegex.IsMatch(version))
            return Result.Fail<ModError>(new ModError.InvalidVersion(version));

        return Result.Ok<ModError>();
    }

    public static Result<ModError> ValidateRequiredFields(ModManifest manifest)
    {
        if (manifest == null)
            return Result.Fail<ModError>(new ModError.RequiredFieldMissing("manifest"));

        if (string.IsNullOrWhiteSpace(manifest.Id))
            return Result.Fail<ModError>(new ModError.RequiredFieldMissing("id"));
        
        if (string.IsNullOrWhiteSpace(manifest.Name))
            return Result.Fail<ModError>(new ModError.RequiredFieldMissing("name"));

        if (string.IsNullOrEmpty(manifest.Author))
            return Result.Fail<ModError>(new ModError.RequiredFieldMissing("author"));
        
        if (string.IsNullOrWhiteSpace(manifest.Version))
            return Result.Fail<ModError>(new ModError.RequiredFieldMissing("version"));
        
        if (manifest.ApiVersion == 0)
            return Result.Fail<ModError>(new ModError.RequiredFieldMissing("apiVersion"));
        
        if (string.IsNullOrWhiteSpace(manifest.EntryPoint))
            return Result.Fail<ModError>(new ModError.RequiredFieldMissing("entryPoint"));
        
        return Result.Ok<ModError>();
    }

    public static Result<ModError> ValidateEntryPoint(string modPath, string entryPoint)
    {
        string fullPath = Path.Combine(modPath, entryPoint);
        if (!File.Exists(fullPath))
            return Result.Fail<ModError>(new ModError.EntryPointNotFound(entryPoint));
        
        if (!fullPath.EndsWith(".dll"))
            return Result.Fail<ModError>(new ModError.InvalidEntryPointExtension(entryPoint));

        return Result.Ok<ModError>();
    }

    public static void CheckFolderNameMismatch(string folderName, string modId)
    {
        var folderNameOnly = Path.GetFileName(folderName);
        var normalizedFolder = NormalizeToSnakeCase(folderNameOnly);
        var normalizedId = NormalizeToSnakeCase(modId);

        if (!string.Equals(normalizedFolder, normalizedId, StringComparison.Ordinal))
        {
            Logger.Here().Warn(
                $"[ModLoader Warning] Folder name '{folderNameOnly}' does not match mod ID '{modId}'. " +
                $"For consistency, folder names should match mod IDs (convention-agnostic comparison)."
            );
        }
    }

    private static string NormalizeToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // Convert CamelCase to snake_case
        var result = Regex.Replace(input, "(?<!^)([A-Z])", "_$1").ToLower();
        return result;
    }

    public static Result<ModError> ValidateApiVersion(int apiVersion, int expectedApiVersion)
    {
        if (apiVersion != expectedApiVersion)
            return Result.Fail<ModError>(new ModError.ApiVersionMismatch(expectedApiVersion, apiVersion));

        return Result.Ok<ModError>();
    }
}