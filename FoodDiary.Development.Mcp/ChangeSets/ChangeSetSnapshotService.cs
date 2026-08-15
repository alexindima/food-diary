using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using FoodDiary.Development.Mcp.Diagnostics;
using FoodDiary.Development.Mcp.Infrastructure;
using FoodDiary.Development.Mcp.Protocol;

namespace FoodDiary.Development.Mcp.ChangeSets;

public sealed class ChangeSetSnapshotService : IChangeSetSnapshotService, IDisposable {
    private static readonly TimeSpan GitTimeout = TimeSpan.FromSeconds(15);
    private readonly string _repositoryRoot = RepositoryRootResolver.Resolve();
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly FileSystemWatcher _watcher;
    private long _generation;
    private long _cachedGeneration = -1;
    private ChangeSetSnapshot? _cached;

    public ChangeSetSnapshotService(TimeProvider timeProvider) {
        _timeProvider = timeProvider;
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
    }

    public ChangeSetSnapshotService() : this(TimeProvider.System) {
    }

    public async Task<ChangeSetSnapshot> GetAsync(CancellationToken cancellationToken) {
        long generation = Interlocked.Read(ref _generation);
        ChangeSetSnapshot? cached = _cached;
        if (cached is not null && generation == Interlocked.Read(ref _cachedGeneration)) {
            return cached;
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try {
            generation = Interlocked.Read(ref _generation);
            if (_cached is not null && generation == Interlocked.Read(ref _cachedGeneration)) {
                return _cached;
            }

            ChangeSetSnapshot snapshot = await CreateAsync(cancellationToken).ConfigureAwait(false);
            _cached = snapshot;
            Interlocked.Exchange(ref _cachedGeneration, generation);
            return snapshot;
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
        string indexPath = Path.Combine(_repositoryRoot, ".git", "index");
        if (File.Exists(indexPath)) {
            hash.AppendData(await File.ReadAllBytesAsync(indexPath, cancellationToken).ConfigureAwait(false));
        }

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
        } catch (OperationCanceledException) when (timeout.IsCancellationRequested) {
            process.Kill(entireProcessTree: true);
            throw new DevelopmentMcpException(
                DevelopmentMcpErrorCodes.Timeout,
                $"Git change-set snapshot exceeded {GitTimeout}.");
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

    private static string[] ParseChangedPaths(string porcelain) {
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
                path = records[++index].Replace('\\', '/');
            }
            paths.Add(path);
        }

        return [.. paths.Order(StringComparer.OrdinalIgnoreCase)];
    }

    private static void Append(IncrementalHash hash, string value) =>
        hash.AppendData(Encoding.UTF8.GetBytes(value));

    private void OnChanged(object sender, FileSystemEventArgs args) {
        string relativePath = Path.GetRelativePath(_repositoryRoot, args.FullPath)
            .Replace('\\', '/');
        if ((relativePath.StartsWith(".git/", StringComparison.OrdinalIgnoreCase) &&
             !relativePath.Equals(".git/HEAD", StringComparison.OrdinalIgnoreCase) &&
             !relativePath.Equals(".git/index", StringComparison.OrdinalIgnoreCase)) ||
            relativePath.StartsWith(".artifacts/", StringComparison.OrdinalIgnoreCase) ||
            relativePath.Contains("/bin/", StringComparison.OrdinalIgnoreCase) ||
            relativePath.Contains("/obj/", StringComparison.OrdinalIgnoreCase)) {
            return;
        }

        Interlocked.Increment(ref _generation);
    }
}
