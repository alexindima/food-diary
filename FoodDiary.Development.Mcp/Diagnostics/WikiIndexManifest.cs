using System.Text.Json;
using FoodDiary.Development.Mcp.Protocol;

namespace FoodDiary.Development.Mcp.Diagnostics;

internal static class WikiIndexManifest {
    private const string RelativePath = ".llm-wiki/policies/query-indexes.json";
    private static readonly JsonSerializerOptions JsonOptions = new() {
        PropertyNameCaseInsensitive = true,
    };

    public static async Task<string[]> ReadPathsAsync(
        string repositoryRoot,
        CancellationToken cancellationToken) {
        string manifestPath = Path.Combine(
            repositoryRoot,
            RelativePath.Replace('/', Path.DirectorySeparatorChar));
        try {
            FileStream stream = File.OpenRead(manifestPath);
            Manifest? manifest;
            await using (stream.ConfigureAwait(false)) {
                manifest = await JsonSerializer.DeserializeAsync<Manifest>(
                    stream,
                    JsonOptions,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            string[] paths = [.. (manifest?.Paths ?? [])
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(path => path.Replace('\\', '/'))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.Ordinal)];
            if (manifest?.SchemaVersion != 1 || paths.Length == 0 ||
                paths.Any(path => Path.IsPathRooted(path) || path.Contains("../", StringComparison.Ordinal))) {
                throw new JsonException("The Wiki query-index manifest is invalid.");
            }

            return paths;
        } catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException) {
            throw new DevelopmentMcpException(
                DevelopmentMcpErrorCodes.WikiUnavailable,
                $"Wiki query-index manifest could not be read at {manifestPath}: {exception.Message}");
        }
    }

    private sealed record Manifest(int SchemaVersion, string[] Paths);
}
