using clypse.portal.setup.Services.IO;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace clypse.portal.setup.Services.Build;

/// <inheritdoc cref="IServiceWorkerAssetHashUpdaterService" />
public partial class ServiceWorkerAssetHashUpdaterService(
    IIoService ioService,
    ILogger<ServiceWorkerAssetHashUpdaterService> logger) : IServiceWorkerAssetHashUpdaterService
{
    private const string ServiceWorkerAssetsFileName = "service-worker-assets.js";

    [GeneratedRegex(@"^self\.assetsManifest\s*=\s*", RegexOptions.Multiline)]
    private static partial Regex AssetsManifestPrefixRegex();

    /// <inheritdoc />
    public async Task<bool> UpdateAssetAsync(
        string publishDirectory,
        string assetPath,
        CancellationToken cancellationToken = default)
    {
        var assetFilePath = ioService.CombinePath(publishDirectory, assetPath);
        var manifestFilePath = ioService.CombinePath(publishDirectory, ServiceWorkerAssetsFileName);

        if (!ValidateFilePaths(assetFilePath, manifestFilePath))
        {
            return false;
        }

        try
        {
            var newHash = await CalculateAssetHashAsync(assetFilePath, assetPath, cancellationToken);
            var manifestJson = await ParseManifestAsync(manifestFilePath, cancellationToken);
            
            if (!UpdateAssetInManifest(manifestJson, assetPath, newHash))
            {
                return false;
            }

            await WriteManifestAsync(manifestFilePath, manifestJson, cancellationToken);
            
            logger.LogInformation(
                "Successfully updated service worker manifest file: '{manifestPath}'",
                manifestFilePath);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error updating asset hash for '{assetPath}' in service worker manifest.",
                assetPath);
            return false;
        }
    }

    private bool ValidateFilePaths(string assetFilePath, string manifestFilePath)
    {
        if (!ioService.FileExists(assetFilePath))
        {
            logger.LogError(
                "Asset file does not exist: '{assetPath}'.",
                assetFilePath);
            return false;
        }

        if (!ioService.FileExists(manifestFilePath))
        {
            logger.LogError(
                "Service worker manifest file does not exist: '{manifestPath}'.",
                manifestFilePath);
            return false;
        }

        return true;
    }

    private async Task<string> CalculateAssetHashAsync(
        string assetFilePath,
        string assetPath,
        CancellationToken cancellationToken)
    {
        var assetBytes = await ioService.ReadAllBytesAsync(assetFilePath, cancellationToken);
        var hashBytes = SHA256.HashData(assetBytes);
        var newHash = $"sha256-{Convert.ToBase64String(hashBytes)}";

        logger.LogInformation(
            "Calculated new hash for asset '{assetPath}': {hash}",
            assetPath,
            newHash);

        return newHash;
    }

    private async Task<JsonNode> ParseManifestAsync(
        string manifestFilePath,
        CancellationToken cancellationToken)
    {
        var manifestContent = await ioService.ReadAllTextAsync(manifestFilePath, cancellationToken);
        var jsonContent = AssetsManifestPrefixRegex().Replace(manifestContent.Trim(), string.Empty).TrimEnd(';');
        
        return JsonNode.Parse(jsonContent) ??
            throw new Exception("Failed to parse service worker manifest JSON.");
    }

    private bool UpdateAssetInManifest(JsonNode manifestJson, string assetPath, string newHash)
    {
        var assetsArray = manifestJson["assets"]?.AsArray();
        if (assetsArray == null)
        {
            logger.LogError("Service worker manifest does not contain 'assets' array.");
            return false;
        }

        var normalizedAssetPath = assetPath.Replace('\\', '/');
        var assetNode = assetsArray
            .FirstOrDefault(a => a?["url"]?.GetValue<string>() == normalizedAssetPath);

        if (assetNode == null)
        {
            logger.LogWarning(
                "Asset '{assetPath}' not found in service worker manifest. It may not be cached.",
                normalizedAssetPath);
            return false;
        }

        assetNode["hash"] = newHash;

        logger.LogInformation(
            "Updated hash for asset '{assetPath}' in service worker manifest.",
            normalizedAssetPath);

        return true;
    }

    private async Task WriteManifestAsync(
        string manifestFilePath,
        JsonNode manifestJson,
        CancellationToken cancellationToken)
    {
        var jsonString = manifestJson.ToJsonString(new JsonSerializerOptions
        {
            WriteIndented = false
        });

        var updatedContent = $"self.assetsManifest = {jsonString};";
        await ioService.WriteAllTextAsync(manifestFilePath, updatedContent, cancellationToken);
    }
}
