using FoodDiary.Development.Mcp.ChangeSets;
using FoodDiary.Development.Mcp.Protocol;

namespace FoodDiary.Development.Mcp.Wiki;

public sealed class WikiQueryService(
    IWikiCommandExecutor executor,
    IChangeSetSnapshotService snapshots,
    WikiQueryCache? queryCache = null) {
    private readonly WikiQueryCache _queryCache = queryCache ??
        new WikiQueryCache(TimeProvider.System, new WikiRuntimeTelemetry());

    public Task<WikiCommandResult> GetTestPlanAsync(
        string? intent,
        IReadOnlyList<string>? plannedPaths,
        IReadOnlyList<string>? changedPaths,
        IReadOnlyList<string>? executedChecks,
        CancellationToken cancellationToken) =>
        GetTestPlanAsync(
            intent,
            plannedPaths,
            changedPaths,
            executedChecks,
            baseRevision: null,
            headRevision: null,
            cancellationToken);

    public Task<DevelopmentContext> GetDevelopmentContextAsync(
        string intent,
        string query,
        string? plannedPath,
        CancellationToken cancellationToken) =>
        GetDevelopmentContextAsync(
            intent,
            query,
            plannedPath,
            baseRevision: null,
            headRevision: null,
            cancellationToken);

    public async Task<WikiCommandResult> GetChangeContextAsync(
        string intent,
        string? plannedPath,
        bool compact,
        CancellationToken cancellationToken) {
        ArgumentException.ThrowIfNullOrWhiteSpace(intent);

        List<string> arguments = ["-Format", "Json", "-Objective", intent];
        if (compact) {
            arguments.Add("-Compact");
        }
        if (!string.IsNullOrWhiteSpace(plannedPath)) {
            arguments.Add("-ProposedPath");
            arguments.Add(plannedPath);
        }

        string[] relevantPaths = NormalizePaths([plannedPath]);
        ChangeSetSnapshot snapshot = await snapshots.GetAsync(relevantPaths, cancellationToken).ConfigureAwait(false);
        AddChangeSet(arguments, snapshot, relevantPaths);

        return await ExecuteCachedAsync("brief", arguments, snapshot, cancellationToken).ConfigureAwait(false);
    }

    public async Task<WikiCommandResult> TraceBackendFlowAsync(
        string query,
        CancellationToken cancellationToken) {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        ChangeSetSnapshot snapshot = await snapshots.GetAsync(cancellationToken).ConfigureAwait(false);
        return await ExecuteCachedAsync(
            "trace",
            ["-Format", "Json", "-Fast", "-Query", query],
            snapshot,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<WikiCommandResult> GetTestPlanAsync(
        string? intent,
        IReadOnlyList<string>? plannedPaths,
        IReadOnlyList<string>? changedPaths,
        IReadOnlyList<string>? executedChecks,
        string? baseRevision,
        string? headRevision,
        CancellationToken cancellationToken) {
        List<string> arguments = string.IsNullOrWhiteSpace(intent)
            ? ["-Format", "Json"]
            : ["-Format", "Json", "-Objective", intent];
        string[] relevantPaths = NormalizePaths((changedPaths ?? []).Concat(plannedPaths ?? []));
        ChangeSetSnapshot snapshot = await snapshots.GetAsync(relevantPaths, cancellationToken).ConfigureAwait(false);
        AddRevisionRange(arguments, snapshot, baseRevision, headRevision, relevantPaths);
        bool hasChangeScope = AddPaths(arguments, "-ChangedPath", changedPaths);
        if (!hasChangeScope) {
            hasChangeScope = AddChangeSet(arguments, snapshot, relevantPaths);
        }
        AddPaths(arguments, "-ProposedPath", plannedPaths);
        AddPaths(arguments, "-ExecutedCheck", executedChecks);

        if (!HasScope(arguments)) {
            throw new DevelopmentMcpException(
                DevelopmentMcpErrorCodes.TestPlanScopeRequired,
                "Provide plannedPaths or changedPaths when the Git worktree is clean.");
        }

        return await ExecuteCachedAsync("test-plan", arguments, snapshot, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<DevelopmentContext> GetDevelopmentContextAsync(
        string intent,
        string query,
        string? plannedPath,
        string? baseRevision,
        string? headRevision,
        CancellationToken cancellationToken) {
        ArgumentException.ThrowIfNullOrWhiteSpace(intent);
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        string[] initialRelevantPaths = NormalizePaths([plannedPath]);
        List<DevelopmentContextComponentError> errors = [];
        ChangeSetSnapshot? snapshot = null;
        WikiCommandResult? trace;
        if (initialRelevantPaths.Length > 0) {
            snapshot = await snapshots.GetAsync(initialRelevantPaths, cancellationToken).ConfigureAwait(false);
            trace = await ExecuteComponentAsync(
                "trace",
                ["-Format", "Json", "-Fast", "-Query", query],
                snapshot,
                errors,
                cancellationToken).ConfigureAwait(false);
        } else {
            trace = await ExecuteComponentUncachedAsync(
                "trace",
                ["-Format", "Json", "-Fast", "-Query", query],
                errors,
                cancellationToken).ConfigureAwait(false);
        }
        string[] expandedScopePaths = [.. new[] { plannedPath }
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Cast<string>()
            .Concat(trace?.GetScopePaths() ?? [])
            .Distinct(StringComparer.OrdinalIgnoreCase)];
        snapshot = await snapshots.GetAsync(expandedScopePaths, cancellationToken).ConfigureAwait(false);

        List<string> briefArguments = [
            "-Format", "Json", "-Compact", "-SkipTestPlan", "-Objective", intent,
        ];
        AddRevisionRange(briefArguments, snapshot, baseRevision, headRevision);
        AddPaths(briefArguments, "-ProposedPath", expandedScopePaths);
        AddChangeSet(briefArguments, snapshot, expandedScopePaths);

        List<string> testArguments = ["-Format", "Json", "-Fast", "-Objective", intent];
        bool baselineAvailable = AddRevisionRange(
            testArguments,
            snapshot,
            baseRevision,
            headRevision,
            expandedScopePaths);
        AddChangeSet(testArguments, snapshot, expandedScopePaths);
        AddPaths(testArguments, "-ProposedPath", expandedScopePaths);

        Task<WikiCommandResult?> briefTask = ExecuteComponentAsync(
            "brief", briefArguments, snapshot, errors, cancellationToken);
        Task<WikiCommandResult?> testPlanTask = HasScope(testArguments)
            ? ExecuteComponentAsync("test-plan", testArguments, snapshot, errors, cancellationToken)
            : Task.FromResult<WikiCommandResult?>(null);
        if (!HasScope(testArguments)) {
            errors.Add(new DevelopmentContextComponentError(
                "test-plan",
                DevelopmentMcpErrorCodes.TestPlanScopeRequired,
                "No changed, planned, or traced repository paths were available for test planning."));
        }
        await Task.WhenAll(briefTask, testPlanTask).ConfigureAwait(false);
        await EnsureSnapshotUnchangedAsync(snapshot, expandedScopePaths, cancellationToken).ConfigureAwait(false);

        string[] effectiveLayers = InferLayers(snapshot.ChangedPaths.Concat(expandedScopePaths));
        return new DevelopmentContext(
            snapshot.Fingerprint,
            snapshot.GitHead,
            await briefTask.ConfigureAwait(false),
            trace,
            await testPlanTask.ConfigureAwait(false),
            PartialSuccess: errors.Count > 0,
            ComponentErrors: errors,
            ExpandedScopePaths: expandedScopePaths,
            ScopeMismatch: HasScopeMismatch(plannedPath, trace?.GetScopePaths()),
            EffectiveLayers: effectiveLayers,
            CrossLayerScope: effectiveLayers.Length > 1,
            BaseRevision: baselineAvailable ? baseRevision ?? snapshot.GitHead : null,
            HeadRevision: headRevision ?? snapshot.GitHead,
            BaselineAvailable: baselineAvailable);
    }

    private async Task EnsureSnapshotUnchangedAsync(
        ChangeSetSnapshot expected,
        IReadOnlyList<string> relevantPaths,
        CancellationToken cancellationToken) {
        ChangeSetSnapshot current = await snapshots.RefreshAsync(relevantPaths, cancellationToken).ConfigureAwait(false);
        if (!string.Equals(expected.Fingerprint, current.Fingerprint, StringComparison.Ordinal)) {
            throw new DevelopmentMcpException(
                DevelopmentMcpErrorCodes.SnapshotChanged,
                "The scoped Git/worktree snapshot changed while development context was being collected. " +
                $"Scope: {string.Join(", ", relevantPaths)}. " +
                $"Before: {expected.Fingerprint[..Math.Min(12, expected.Fingerprint.Length)]}; " +
                $"after: {current.Fingerprint[..Math.Min(12, current.Fingerprint.Length)]}. Retry the request.");
        }
    }

    private async Task<WikiCommandResult?> ExecuteComponentAsync(
        string command,
        IReadOnlyList<string> arguments,
        ChangeSetSnapshot snapshot,
        List<DevelopmentContextComponentError> errors,
        CancellationToken cancellationToken) {
        try {
            return await ExecuteCachedAsync(command, arguments, snapshot, cancellationToken)
                .ConfigureAwait(false);
        } catch (DevelopmentMcpException exception) {
            lock (errors) {
                errors.Add(new DevelopmentContextComponentError(command, exception.ErrorCode, exception.Message));
            }
            return null;
        }
    }

    private async Task<WikiCommandResult?> ExecuteComponentUncachedAsync(
        string command,
        IReadOnlyList<string> arguments,
        List<DevelopmentContextComponentError> errors,
        CancellationToken cancellationToken) {
        try {
            return await executor.ExecuteAsync(command, arguments, cancellationToken)
                .ConfigureAwait(false);
        } catch (DevelopmentMcpException exception) {
            lock (errors) {
                errors.Add(new DevelopmentContextComponentError(command, exception.ErrorCode, exception.Message));
            }
            return null;
        }
    }

    private async Task<WikiCommandResult> ExecuteCachedAsync(
        string command,
        IReadOnlyList<string> arguments,
        ChangeSetSnapshot snapshot,
        CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        if (_queryCache.TryGet(snapshot.Fingerprint, command, arguments, out WikiCommandResult? cached)) {
            return cached!;
        }

        WikiCommandResult? result = await executor.ExecuteAsync(command, arguments, cancellationToken)
            .ConfigureAwait(false);
        if (result is not null) {
            _queryCache.Set(snapshot.Fingerprint, command, arguments, result);
        }
        return result!;
    }

    private static bool AddChangeSet(
        List<string> arguments,
        ChangeSetSnapshot snapshot,
        IReadOnlyList<string>? relevantPaths = null) {
        string[] changedPaths = GetRelevantChangedPaths(snapshot, relevantPaths);
        if (changedPaths.Length == 0) {
            return false;
        }

        AddPaths(arguments, "-ChangedPath", changedPaths);
        return true;
    }

    private static string[] NormalizePaths(IEnumerable<string?> paths) => [.. paths
        .Where(path => !string.IsNullOrWhiteSpace(path))
        .Cast<string>()
        .Select(path => path.Replace('\\', '/').TrimEnd('/'))
        .Distinct(StringComparer.OrdinalIgnoreCase)];

    private static string[] GetRelevantChangedPaths(
        ChangeSetSnapshot snapshot,
        IReadOnlyList<string>? relevantPaths) =>
        relevantPaths is null || relevantPaths.Count == 0
            ? [.. snapshot.ChangedPaths]
            : [.. snapshot.ChangedPaths.Where(path =>
                ChangeSetSnapshotService.IsPathRelevantToScope(path, relevantPaths))];

    private static bool AddPaths(
        List<string> arguments,
        string parameter,
        IReadOnlyList<string>? paths) {
        string[] normalized = [.. (paths ?? [])
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)];
        foreach (string path in normalized) {
            arguments.Add(parameter);
            arguments.Add(path);
        }
        return normalized.Length > 0;
    }

    private static bool HasScope(IReadOnlyList<string> arguments) =>
        arguments.Contains("-ChangedPath", StringComparer.Ordinal) ||
        arguments.Contains("-ProposedPath", StringComparer.Ordinal);

    private static bool AddRevisionRange(
        List<string> arguments,
        ChangeSetSnapshot snapshot,
        string? baseRevision,
        string? headRevision,
        IReadOnlyList<string>? relevantPaths = null) {
        if (!string.IsNullOrWhiteSpace(baseRevision) &&
            !string.IsNullOrWhiteSpace(headRevision) &&
            string.Equals(baseRevision, headRevision, StringComparison.OrdinalIgnoreCase)) {
            throw new DevelopmentMcpException(
                DevelopmentMcpErrorCodes.InvalidRevisionRange,
                "baseRevision and headRevision must identify different revisions.");
        }
        bool baselineAvailable = !string.IsNullOrWhiteSpace(baseRevision) ||
            GetRelevantChangedPaths(snapshot, relevantPaths).Length > 0;
        if (baselineAvailable) {
            arguments.Add("-BaseRef");
            arguments.Add(baseRevision ?? snapshot.GitHead);
        } else {
            arguments.Add("-NoBaseline");
        }
        if (!string.IsNullOrWhiteSpace(headRevision)) {
            arguments.Add("-HeadRef");
            arguments.Add(headRevision);
        }
        return baselineAvailable;
    }

    private static bool HasScopeMismatch(string? plannedPath, IReadOnlyList<string>? tracedPaths) {
        if (string.IsNullOrWhiteSpace(plannedPath)) {
            return false;
        }

        string normalizedPlannedPath = plannedPath.Replace('\\', '/').TrimEnd('/');
        return (tracedPaths ?? []).Any(path => {
            string normalizedPath = path.Replace('\\', '/');
            return !normalizedPath.Equals(normalizedPlannedPath, StringComparison.OrdinalIgnoreCase) &&
                !normalizedPath.StartsWith($"{normalizedPlannedPath}/", StringComparison.OrdinalIgnoreCase);
        });
    }

    private static string[] InferLayers(IEnumerable<string> paths) => [.. paths
        .Select(InferLayer)
        .Where(layer => layer is not null)
        .Cast<string>()
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Order(StringComparer.OrdinalIgnoreCase)];

    private static string? InferLayer(string path) {
        string normalized = path.Replace('\\', '/');
        if (normalized.StartsWith("FoodDiary.Web.Client/", StringComparison.OrdinalIgnoreCase)) {
            return "Frontend";
        }
        if (normalized.StartsWith("FoodDiary.Presentation.Api/", StringComparison.OrdinalIgnoreCase) ||
            normalized.StartsWith("FoodDiary.Web.", StringComparison.OrdinalIgnoreCase)) {
            return "Api";
        }
        if (normalized.StartsWith("FoodDiary.Infrastructure/", StringComparison.OrdinalIgnoreCase)) {
            return "Infrastructure";
        }
        if (normalized.StartsWith("FoodDiary.Domain/", StringComparison.OrdinalIgnoreCase)) {
            return "Domain";
        }
        if (normalized.StartsWith("FoodDiary.Application", StringComparison.OrdinalIgnoreCase)) {
            return "Application";
        }
        return null;
    }
}
