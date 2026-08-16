using FoodDiary.Development.Mcp.Infrastructure;
using FoodDiary.Development.Mcp.Protocol;
using FoodDiary.Development.Mcp.ChangeSets;
using System.Security.Cryptography;

namespace FoodDiary.Development.Mcp.Diagnostics;

public sealed class ServerStatusService(
    TimeProvider timeProvider,
    ServerRuntimeIdentity runtimeIdentity,
    IChangeSetSnapshotService snapshots) : IServerStatusService {
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
        ChangeSetSnapshot snapshot = await snapshots.GetAsync(cancellationToken).ConfigureAwait(false);
        string[] derivedWikiPaths = [.. snapshot.ChangedPaths.Where(IsDerivedWikiPath)];
        string[] reviewMetadataPaths = [.. snapshot.ChangedPaths.Where(IsReviewMetadataPath)];
        string[] sourceChangedPaths = [.. snapshot.ChangedPaths.Except(derivedWikiPaths, StringComparer.OrdinalIgnoreCase)
            .Except(reviewMetadataPaths, StringComparer.OrdinalIgnoreCase)];
        WikiIndexStatus[] indexes = [.. IndexPaths.Select(path => {
            string absolutePath = Path.Combine(repositoryRoot, path.Replace('/', Path.DirectorySeparatorChar));
            FileInfo file = new(absolutePath);
            return new WikiIndexStatus(
                path,
                file.Exists,
                file.Exists ? new DateTimeOffset(file.LastWriteTimeUtc) : null);
        })];
        bool indexFilesPresent = indexes.All(index => index.Exists);
        bool indexesMatchWorktree = indexFilesPresent && AreIndexesCurrentForSources(
            repositoryRoot,
            indexes,
            sourceChangedPaths);
        string? indexFingerprint = indexFilesPresent
            ? await ComputeIndexFingerprintAsync(repositoryRoot, cancellationToken).ConfigureAwait(false)
            : null;
        string indexCheckSummary = !indexFilesPresent
            ? "One or more required generated indexes are missing."
            : $"Required index files exist; content fingerprint is {indexFingerprint}. Source-to-index freshness was not regenerated.";

        return new ServerStatus(
            typeof(ServerStatusService).Assembly.GetName().Version?.ToString() ?? "unknown",
            runtimeIdentity,
            repositoryRoot,
            gitHead,
            string.Equals(runtimeIdentity.RepositoryHeadAtStartup, gitHead, StringComparison.Ordinal),
            WorktreeDirty: snapshot.ChangedPaths.Count > 0,
            WorktreeFingerprint: snapshot.Fingerprint,
            IndexesMatchWorktree: indexesMatchWorktree,
            RunningCodeIncludesWorktreeChanges: snapshot.ChangedPaths.Count == 0,
            sourceChangedPaths,
            derivedWikiPaths,
            reviewMetadataPaths,
            WikiAvailable: true,
            indexFilesPresent,
            DeepFreshness: indexFilesPresent ? "fingerprinted" : "missing",
            LastVerifiedCommit: null,
            indexFingerprint,
            indexFilesPresent ? "present" : DevelopmentMcpErrorCodes.IndexStale,
            indexCheckSummary,
            indexes,
            timeProvider.GetUtcNow());
    }

    private static bool IsDerivedWikiPath(string path) =>
        path.StartsWith(".llm-wiki/generated/", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith(".llm-wiki/.generated/", StringComparison.OrdinalIgnoreCase);

    private static bool IsReviewMetadataPath(string path) =>
        path.StartsWith(".llm-wiki/reviews/", StringComparison.OrdinalIgnoreCase) ||
        path.Contains("review-receipt", StringComparison.OrdinalIgnoreCase) ||
        path.Contains("source-impact-review", StringComparison.OrdinalIgnoreCase);

    private static bool AreIndexesCurrentForSources(
        string repositoryRoot,
        IReadOnlyList<WikiIndexStatus> indexes,
        IReadOnlyList<string> sourceChangedPaths) {
        if (sourceChangedPaths.Count == 0) {
            return true;
        }

        DateTimeOffset newestSourceWrite = sourceChangedPaths
            .Select(path => Path.Combine(repositoryRoot, path.Replace('/', Path.DirectorySeparatorChar)))
            .Where(File.Exists)
            .Select(path => new DateTimeOffset(File.GetLastWriteTimeUtc(path)))
            .DefaultIfEmpty(DateTimeOffset.MaxValue)
            .Max();
        return indexes.All(index => index.LastWriteTimeUtc >= newestSourceWrite);
    }

    private static async Task<string> ComputeIndexFingerprintAsync(
        string repositoryRoot,
        CancellationToken cancellationToken) {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (string path in IndexPaths.Order(StringComparer.Ordinal)) {
            hash.AppendData(System.Text.Encoding.UTF8.GetBytes(path));
            FileStream stream = File.OpenRead(Path.Combine(
                repositoryRoot,
                path.Replace('/', Path.DirectorySeparatorChar)));
            await using (stream.ConfigureAwait(false)) {
                byte[] buffer = new byte[81920];
                int read;
                while ((read = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0) {
                    hash.AppendData(buffer.AsSpan(0, read));
                }
            }
        }
        return Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
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
