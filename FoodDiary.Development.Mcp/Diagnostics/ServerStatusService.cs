using FoodDiary.Development.Mcp.Infrastructure;
using FoodDiary.Development.Mcp.Protocol;

namespace FoodDiary.Development.Mcp.Diagnostics;

public sealed class ServerStatusService(TimeProvider timeProvider) : IServerStatusService {
    private static readonly string[] IndexPaths = [
        ".llm-wiki/generated/repository-catalog.json",
        ".llm-wiki/generated/csharp-symbol-index.json",
        ".llm-wiki/generated/backend-contract-index.json",
        ".llm-wiki/generated/quality-index.json",
        ".llm-wiki/generated/architecture-health-index.json",
    ];

    public async Task<ServerStatus> GetStatusAsync(CancellationToken cancellationToken) {
        string repositoryRoot = RepositoryRootResolver.Resolve();
        string wikiPath = Path.Combine(repositoryRoot, ".llm-wiki", "wiki.ps1");
        if (!File.Exists(wikiPath)) {
            throw new DevelopmentMcpException(
                DevelopmentMcpErrorCodes.WikiUnavailable,
                $"Wiki entrypoint was not found at {wikiPath}.");
        }

        string gitHead = await ReadGitHeadAsync(repositoryRoot, cancellationToken).ConfigureAwait(false);
        WikiIndexStatus[] indexes = [.. IndexPaths.Select(path => {
            string absolutePath = Path.Combine(repositoryRoot, path.Replace('/', Path.DirectorySeparatorChar));
            FileInfo file = new(absolutePath);
            return new WikiIndexStatus(
                path,
                file.Exists,
                file.Exists ? new DateTimeOffset(file.LastWriteTimeUtc) : null);
        })];
        bool indexesStale = indexes.Any(index => !index.Exists);
        string indexCheckSummary = indexesStale
            ? "One or more required generated indexes are missing."
            : "Required indexes exist. Deep freshness is enforced by wiki verify, not the health endpoint.";

        return new ServerStatus(
            typeof(ServerStatusService).Assembly.GetName().Version?.ToString() ?? "unknown",
            repositoryRoot,
            gitHead,
            WikiAvailable: true,
            indexesStale,
            indexesStale ? DevelopmentMcpErrorCodes.IndexStale : "current",
            indexCheckSummary,
            indexes,
            timeProvider.GetUtcNow());
    }

    public static async Task<string> ReadGitHeadAsync(
        string repositoryRoot,
        CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        string gitDirectory = await ResolveGitDirectoryAsync(repositoryRoot, cancellationToken).ConfigureAwait(false);
        string head = (await File.ReadAllTextAsync(
            Path.Combine(gitDirectory, "HEAD"),
            cancellationToken).ConfigureAwait(false)).Trim();
        if (!head.StartsWith("ref: ", StringComparison.Ordinal)) {
            return head;
        }

        string reference = head[5..];
        string referencePath = Path.Combine(gitDirectory, reference.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(referencePath)) {
            return (await File.ReadAllTextAsync(referencePath, cancellationToken).ConfigureAwait(false)).Trim();
        }

        return (await File.ReadAllLinesAsync(
            Path.Combine(gitDirectory, "packed-refs"),
            cancellationToken).ConfigureAwait(false))
            .FirstOrDefault(line => line.EndsWith($" {reference}", StringComparison.Ordinal))
            ?.Split(' ', 2)[0]
            ?? throw new DevelopmentMcpException(
                DevelopmentMcpErrorCodes.RepositoryNotFound,
                $"Git reference '{reference}' could not be resolved.");
    }

    private static async Task<string> ResolveGitDirectoryAsync(
        string repositoryRoot,
        CancellationToken cancellationToken) {
        string dotGitPath = Path.Combine(repositoryRoot, ".git");
        if (Directory.Exists(dotGitPath)) {
            return dotGitPath;
        }

        string marker = (await File.ReadAllTextAsync(dotGitPath, cancellationToken).ConfigureAwait(false)).Trim();
        const string prefix = "gitdir: ";
        if (!marker.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) {
            throw new DevelopmentMcpException(
                DevelopmentMcpErrorCodes.RepositoryNotFound,
                $"Invalid Git directory marker at {dotGitPath}.");
        }

        return Path.GetFullPath(marker[prefix.Length..], repositoryRoot);
    }
}
