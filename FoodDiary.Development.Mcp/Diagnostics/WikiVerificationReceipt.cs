using System.Text.Json;

namespace FoodDiary.Development.Mcp.Diagnostics;

internal sealed record WikiVerificationReceipt(
    int SchemaVersion,
    string GitHead,
    string SourceFingerprint,
    string IndexFingerprint,
    DateTimeOffset VerifiedAtUtc) {
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task<WikiVerificationReceipt?> ReadAsync(
        string repositoryRoot,
        CancellationToken cancellationToken) {
        string gitDirectory = await ServerStatusService
            .ResolveGitDirectoryForStatusAsync(repositoryRoot, cancellationToken)
            .ConfigureAwait(false);
        string path = Path.Combine(gitDirectory, "llm-wiki", "index-verification.json");
        if (!File.Exists(path)) {
            return null;
        }
        try {
            FileStream stream = File.OpenRead(path);
            await using (stream.ConfigureAwait(false)) {
                WikiVerificationReceipt? receipt = await JsonSerializer
                    .DeserializeAsync<WikiVerificationReceipt>(stream, JsonOptions, cancellationToken)
                    .ConfigureAwait(false);
                return receipt?.SchemaVersion == 1 ? receipt : null;
            }
        } catch (JsonException) {
            return null;
        }
    }
}
