using System.Diagnostics;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using FoodDiary.Development.Mcp.Diagnostics;
using FoodDiary.Development.Mcp.Infrastructure;
using FoodDiary.Development.Mcp.Protocol;

namespace FoodDiary.Development.Mcp.ChangeSets;

public sealed class ChangeSetSnapshotService : IChangeSetSnapshotService, IDisposable {
    private static readonly TimeSpan GitTimeout = TimeSpan.FromSeconds(15);
    private readonly string _repositoryRoot;
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly FileSystemWatcher _watcher;
    private long _generation;
    private long _cachedGeneration = -1;
    private ChangeSetSnapshot? _cached;
    private ConcurrentDictionary<string, byte> _pendingChangedPaths = new(StringComparer.OrdinalIgnoreCase);

    internal ChangeSetSnapshotService(TimeProvider timeProvider, string repositoryRoot) {
        _timeProvider = timeProvider;
        _repositoryRoot = repositoryRoot;
        _watcher = new FileSystemWatcher(_repositoryRoot) {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName |
                NotifyFilters.LastWrite | NotifyFilters.Size,
            EnableRaisingEvents = true,
        };
        _watcher.Changed += OnChanged;
        _watcher.Created += OnChanged;
        _watcher.Deleted += OnChanged;
        _watcher.Renamed += OnChanged;
        _watcher.Error += OnWatcherError;
    }

    public ChangeSetSnapshotService(TimeProvider timeProvider)
        : this(timeProvider, RepositoryRootResolver.Resolve()) {
    }

    public ChangeSetSnapshotService() : this(TimeProvider.System) {
    }

    public Task<ChangeSetSnapshot> GetAsync(CancellationToken cancellationToken) =>
        GetAsync(relevantPaths: null, cancellationToken);

    public async Task<ChangeSetSnapshot> GetAsync(
        IReadOnlyList<string>? relevantPaths,
        CancellationToken cancellationToken) {
        long generation = Interlocked.Read(ref _generation);
        ChangeSetSnapshot? cached = _cached;
        if (cached is not null &&
            (generation == Interlocked.Read(ref _cachedGeneration) || !HasRelevantPendingChange(relevantPaths)) &&
            await MatchesCurrentHeadAsync(cached, cancellationToken).ConfigureAwait(false)) {
            return await ProjectToScopeAsync(cached, relevantPaths, cancellationToken).ConfigureAwait(false);
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try {
            generation = Interlocked.Read(ref _generation);
            if (_cached is not null &&
                (generation == Interlocked.Read(ref _cachedGeneration) || !HasRelevantPendingChange(relevantPaths)) &&
                await MatchesCurrentHeadAsync(_cached, cancellationToken).ConfigureAwait(false)) {
                return await ProjectToScopeAsync(_cached, relevantPaths, cancellationToken).ConfigureAwait(false);
            }

            _ = Interlocked.Exchange(
                ref _pendingChangedPaths,
                new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase));
            ChangeSetSnapshot snapshot = await CreateAsync(cancellationToken).ConfigureAwait(false);
            _cached = snapshot;
            Interlocked.Exchange(ref _cachedGeneration, generation);
            return await ProjectToScopeAsync(snapshot, relevantPaths, cancellationToken).ConfigureAwait(false);
        } finally {
            _gate.Release();
        }
    }

    public Task<ChangeSetSnapshot> RefreshAsync(CancellationToken cancellationToken) =>
        RefreshAsync(relevantPaths: null, cancellationToken);

    public async Task<ChangeSetSnapshot> RefreshAsync(
        IReadOnlyList<string>? relevantPaths,
        CancellationToken cancellationToken) {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try {
            long generation = Interlocked.Read(ref _generation);
            if (_cached is not null && relevantPaths is { Count: > 0 } &&
                !HasRelevantPendingChange(relevantPaths) &&
                await MatchesCurrentHeadAsync(_cached, cancellationToken).ConfigureAwait(false)) {
                return await ProjectToScopeAsync(_cached, relevantPaths, cancellationToken).ConfigureAwait(false);
            }
            _ = Interlocked.Exchange(
                ref _pendingChangedPaths,
                new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase));
            ChangeSetSnapshot snapshot = await CreateAsync(cancellationToken).ConfigureAwait(false);
            _cached = snapshot;
            Interlocked.Exchange(ref _cachedGeneration, generation);
            return await ProjectToScopeAsync(snapshot, relevantPaths, cancellationToken).ConfigureAwait(false);
        } finally {
            _gate.Release();
        }
    }

    public void Dispose() {
        _watcher.Dispose();
        _gate.Dispose();
    }

    private async Task<ChangeSetSnapshot> CreateAsync(CancellationToken cancellationToken) {
        string head = await ServerStatusService.ReadGitHeadAsync(_repositoryRoot, cancellationToken)
            .ConfigureAwait(false);
        string status = await RunGitAsync(
            ["status", "--porcelain=v1", "-z", "--untracked-files=all"],
            cancellationToken).ConfigureAwait(false);
        string[] changedPaths = ParseChangedPaths(status);
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Append(hash, head);
        Append(hash, status);

        foreach (string path in changedPaths) {
            Append(hash, path);
            string absolutePath = Path.Combine(
                _repositoryRoot,
                path.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(absolutePath)) {
                FileStream stream = File.OpenRead(absolutePath);
                await using ConfiguredAsyncDisposable configuredStream = stream.ConfigureAwait(false);
                byte[] contentHash = await SHA256.HashDataAsync(stream, cancellationToken)
                    .ConfigureAwait(false);
                hash.AppendData(contentHash);
            } else {
                Append(hash, "<missing>");
            }
        }

        return new ChangeSetSnapshot(
            head,
            Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant(),
            changedPaths,
            _timeProvider.GetUtcNow());
    }

    private async Task<ChangeSetSnapshot> ProjectToScopeAsync(
        ChangeSetSnapshot snapshot,
        IReadOnlyList<string>? relevantPaths,
        CancellationToken cancellationToken) {
        if (relevantPaths is not { Count: > 0 }) {
            return snapshot;
        }

        string[] changedPaths = [.. snapshot.ChangedPaths.Where(path =>
            IsPathRelevantToScope(path, relevantPaths))];
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Append(hash, snapshot.GitHead);
        foreach (string path in changedPaths) {
            Append(hash, path);
            string absolutePath = Path.Combine(
                _repositoryRoot,
                path.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(absolutePath)) {
                FileStream stream = File.OpenRead(absolutePath);
                await using ConfiguredAsyncDisposable configuredStream = stream.ConfigureAwait(false);
                byte[] contentHash = await SHA256.HashDataAsync(stream, cancellationToken)
                    .ConfigureAwait(false);
                hash.AppendData(contentHash);
            } else {
                Append(hash, "<missing>");
            }
        }

        return snapshot with {
            Fingerprint = Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant(),
            ChangedPaths = changedPaths,
        };
    }

    private async Task<string> RunGitAsync(
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken) {
        using Process process = new() {
            StartInfo = new ProcessStartInfo {
                FileName = "git",
                WorkingDirectory = _repositoryRoot,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            },
        };
        GitProcessEnvironment.ClearLocalRepositoryVariables(process.StartInfo);
        process.StartInfo.ArgumentList.Add("-c");
        process.StartInfo.ArgumentList.Add("core.fsmonitor=false");
        foreach (string argument in arguments) {
            process.StartInfo.ArgumentList.Add(argument);
        }

        process.Start();
        process.StandardInput.Close();
        Task<string> standardOutput = process.StandardOutput.ReadToEndAsync(cancellationToken);
        Task<string> standardError = process.StandardError.ReadToEndAsync(cancellationToken);
        using CancellationTokenSource timeout = new(GitTimeout);
        using var linkedCancellation =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
        try {
            await process.WaitForExitAsync(linkedCancellation.Token).ConfigureAwait(false);
        } catch (OperationCanceledException) {
            TryKill(process);
            if (timeout.IsCancellationRequested) {
                throw new DevelopmentMcpException(
                    DevelopmentMcpErrorCodes.Timeout,
                    $"Git change-set snapshot exceeded {GitTimeout}.");
            }
            throw;
        }

        string output = await standardOutput.ConfigureAwait(false);
        string error = await standardError.ConfigureAwait(false);
        if (process.ExitCode != 0) {
            throw new DevelopmentMcpException(
                DevelopmentMcpErrorCodes.RepositoryNotFound,
                $"Git change-set snapshot failed: {error.Trim()}");
        }

        return output;
    }

    internal static string[] ParseChangedPaths(string porcelain) {
        string[] records = porcelain.Split('\0', StringSplitOptions.RemoveEmptyEntries);
        HashSet<string> paths = new(StringComparer.OrdinalIgnoreCase);
        for (int index = 0; index < records.Length; index++) {
            string record = records[index];
            if (record.Length < 4) {
                continue;
            }

            string status = record[..2];
            string path = record[3..].Replace('\\', '/');
            if ((status[0] is 'R' or 'C' || status[1] is 'R' or 'C') && index + 1 < records.Length) {
                // In porcelain v1 -z output the destination path is first and the source path follows it.
                index++;
            }
            paths.Add(path);
        }

        return [.. paths.Order(StringComparer.OrdinalIgnoreCase)];
    }

    private static void Append(IncrementalHash hash, string value) =>
        hash.AppendData(Encoding.UTF8.GetBytes(value));

    private async Task<bool> MatchesCurrentHeadAsync(
        ChangeSetSnapshot snapshot,
        CancellationToken cancellationToken) =>
        string.Equals(
            snapshot.GitHead,
            await ServerStatusService.ReadGitHeadAsync(_repositoryRoot, cancellationToken).ConfigureAwait(false),
            StringComparison.Ordinal);

    internal static bool IsSourcePath(string path) =>
        !path.StartsWith(".llm-wiki/generated/", StringComparison.OrdinalIgnoreCase) &&
        !path.StartsWith(".llm-wiki/reviews/", StringComparison.OrdinalIgnoreCase) &&
        !path.StartsWith(".artifacts/", StringComparison.OrdinalIgnoreCase) &&
        !path.Contains("review-receipt", StringComparison.OrdinalIgnoreCase) &&
        !path.Contains("source-impact-review", StringComparison.OrdinalIgnoreCase);

    private void OnChanged(object sender, FileSystemEventArgs args) {
        string relativePath = Path.GetRelativePath(_repositoryRoot, args.FullPath)
            .Replace('\\', '/');
        if (IsIgnoredWatcherPath(relativePath)) {
            return;
        }

        Volatile.Read(ref _pendingChangedPaths)[relativePath] = 0;
        Interlocked.Increment(ref _generation);
    }

    private void OnWatcherError(object sender, ErrorEventArgs args) {
        Volatile.Read(ref _pendingChangedPaths)["*"] = 0;
        Interlocked.Increment(ref _generation);
    }

    private bool HasRelevantPendingChange(IReadOnlyList<string>? relevantPaths) {
        string[] pendingPaths = [.. Volatile.Read(ref _pendingChangedPaths).Keys];
        if (pendingPaths.Length == 0) {
            return false;
        }
        if (relevantPaths is null || relevantPaths.Count == 0) {
            return true;
        }
        return pendingPaths.Any(path => IsPathRelevantToScope(path, relevantPaths));
    }

    internal static bool IsPathRelevantToScope(
        string path,
        IReadOnlyList<string> relevantPaths) {
        string candidate = path.Replace('\\', '/').Trim('/');
        if (candidate is "*" ||
            candidate.Equals(".git/HEAD", StringComparison.OrdinalIgnoreCase) ||
            candidate.Equals(".git/index", StringComparison.OrdinalIgnoreCase)) {
            return true;
        }
        foreach (string relevantPath in relevantPaths) {
            string scope = relevantPath.Replace('\\', '/').Trim('/');
            if (string.IsNullOrWhiteSpace(scope)) {
                continue;
            }
            if (candidate.Equals(scope, StringComparison.OrdinalIgnoreCase) ||
                candidate.StartsWith($"{scope}/", StringComparison.OrdinalIgnoreCase) ||
                scope.StartsWith($"{candidate}/", StringComparison.OrdinalIgnoreCase)) {
                return true;
            }
        }
        return false;
    }

    internal static bool IsIgnoredWatcherPath(string path) {
        string normalized = $"/{path.Replace('\\', '/').Trim('/')}/";
        if (normalized.StartsWith("/.git/", StringComparison.OrdinalIgnoreCase)) {
            return !normalized.Equals("/.git/HEAD/", StringComparison.OrdinalIgnoreCase) &&
                !normalized.Equals("/.git/index/", StringComparison.OrdinalIgnoreCase);
        }

        string[] ignoredSegments = [
            ".artifacts", "bin", "obj", "node_modules", "dist", ".angular",
            "coverage", "TestResults",
        ];
        return ignoredSegments.Any(segment =>
            normalized.Contains($"/{segment}/", StringComparison.OrdinalIgnoreCase));
    }

    private static void TryKill(Process process) {
        try {
            if (!process.HasExited) {
                process.Kill(entireProcessTree: true);
            }
        } catch (InvalidOperationException) {
        }
    }
}
