using System.Diagnostics;

namespace FoodDiary.Development.Mcp;

public sealed class ServerStatusService : IServerStatusService {
    private static readonly TimeSpan IndexStatusLifetime = TimeSpan.FromSeconds(30);
    private static readonly string[] IndexPaths = [
        ".llm-wiki/generated/repository-catalog.json",
        ".llm-wiki/generated/csharp-symbol-index.json",
        ".llm-wiki/generated/backend-contract-index.json",
        ".llm-wiki/generated/quality-index.json",
        ".llm-wiki/generated/architecture-health-index.json",
    ];
    private readonly SemaphoreSlim _indexGate = new(1, 1);
    private (DateTimeOffset CreatedAtUtc, bool Stale, string Summary)? _cachedIndexStatus;

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
        (bool indexesStale, string indexCheckSummary) =
            await GetIndexStatusAsync(repositoryRoot, indexes, cancellationToken).ConfigureAwait(false);

        return new ServerStatus(
            typeof(ServerStatusService).Assembly.GetName().Version?.ToString() ?? "unknown",
            repositoryRoot,
            gitHead,
            WikiAvailable: true,
            indexesStale,
            indexesStale ? DevelopmentMcpErrorCodes.IndexStale : "current",
            indexCheckSummary,
            indexes,
            DateTimeOffset.UtcNow);
    }

    private async Task<(bool Stale, string Summary)> GetIndexStatusAsync(
        string repositoryRoot,
        IReadOnlyList<WikiIndexStatus> indexes,
        CancellationToken cancellationToken) {
        (DateTimeOffset CreatedAtUtc, bool Stale, string Summary)? cached = _cachedIndexStatus;
        if (cached is not null && DateTimeOffset.UtcNow - cached.Value.CreatedAtUtc < IndexStatusLifetime) {
            return (cached.Value.Stale, cached.Value.Summary);
        }

        await _indexGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try {
            cached = _cachedIndexStatus;
            if (cached is not null && DateTimeOffset.UtcNow - cached.Value.CreatedAtUtc < IndexStatusLifetime) {
                return (cached.Value.Stale, cached.Value.Summary);
            }

            (bool stale, string summary) =
                await CheckIndexesAsync(repositoryRoot, indexes, cancellationToken).ConfigureAwait(false);
            _cachedIndexStatus = (DateTimeOffset.UtcNow, stale, summary);
            return (stale, summary);
        } finally {
            _indexGate.Release();
        }
    }

    public static async Task<string> ReadGitHeadAsync(
        string repositoryRoot,
        CancellationToken cancellationToken) {
        using Process process = new() {
            StartInfo = new ProcessStartInfo {
                FileName = "git",
                WorkingDirectory = repositoryRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            },
        };
        process.StartInfo.ArgumentList.Add("rev-parse");
        process.StartInfo.ArgumentList.Add("HEAD");
        if (!process.Start()) {
            throw new DevelopmentMcpException(
                DevelopmentMcpErrorCodes.RepositoryNotFound,
                "Git could not be started to resolve HEAD.");
        }

        Task<string> standardOutput = process.StandardOutput.ReadToEndAsync(cancellationToken);
        Task<string> standardError = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        string output = await standardOutput.ConfigureAwait(false);
        string error = await standardError.ConfigureAwait(false);
        if (process.ExitCode != 0) {
            throw new DevelopmentMcpException(
                DevelopmentMcpErrorCodes.RepositoryNotFound,
                $"Git HEAD could not be resolved: {error.Trim()}");
        }

        return output.Trim();
    }

    private static async Task<(bool Stale, string Summary)> CheckIndexesAsync(
        string repositoryRoot,
        IReadOnlyList<WikiIndexStatus> indexes,
        CancellationToken cancellationToken) {
        if (indexes.Any(index => !index.Exists)) {
            return (true, "One or more required generated indexes are missing.");
        }

        string checkerPath = Path.Combine(
            repositoryRoot,
            ".llm-wiki",
            "tools",
            "Invoke-LlmWikiIndexPipeline.ps1");
        using Process process = new() {
            StartInfo = new ProcessStartInfo {
                FileName = OperatingSystem.IsWindows() ? "powershell.exe" : "pwsh",
                WorkingDirectory = repositoryRoot,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            },
        };
        foreach (string argument in new[] {
            "-NoLogo",
            "-NoProfile",
            "-NonInteractive",
            "-File",
            checkerPath,
            "-Check",
            "-AffectedOnly",
            "-ReuseUnchangedChecks",
        }) {
            process.StartInfo.ArgumentList.Add(argument);
        }

        if (!process.Start()) {
            return (true, "The index freshness checker could not be started.");
        }
        process.StandardInput.Close();
        Task<string> standardOutput = process.StandardOutput.ReadToEndAsync(cancellationToken);
        Task<string> standardError = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        string output = await standardOutput.ConfigureAwait(false);
        string error = await standardError.ConfigureAwait(false);
        string summary = string.Join(
            Environment.NewLine,
            new[] { output.Trim(), error.Trim() }.Where(value => !string.IsNullOrWhiteSpace(value)));
        return (process.ExitCode != 0, summary);
    }
}
