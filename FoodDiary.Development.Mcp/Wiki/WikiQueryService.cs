using FoodDiary.Development.Mcp.ChangeSets;
using FoodDiary.Development.Mcp.Protocol;

namespace FoodDiary.Development.Mcp.Wiki;

public sealed class WikiQueryService(
    IWikiCommandExecutor executor,
    IChangeSetSnapshotService snapshots,
    WikiQueryCache? queryCache = null,
    IWikiContextSearch? contextSearch = null,
    WikiRuntimeTelemetry? telemetry = null) {
    private readonly WikiQueryCache _queryCache = queryCache ??
        new WikiQueryCache(TimeProvider.System, new WikiRuntimeTelemetry());
    private readonly WikiRuntimeTelemetry _telemetry = telemetry ?? new WikiRuntimeTelemetry();

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

        var routingStopwatch = System.Diagnostics.Stopwatch.StartNew();
        ChangeSetSnapshot fullSnapshot = await snapshots
            .GetAsync(relevantPaths: null, cancellationToken)
            .ConfigureAwait(false);
        WikiContextSearchResult? sqlContext = contextSearch is null
            ? null
            : await SearchSqlContextAsync(
                contextSearch,
                query,
                plannedPath,
                fullSnapshot.Fingerprint,
                cancellationToken).ConfigureAwait(false);
        string? refreshFailureReason = null;
        bool refreshAttempted = false;
        bool refreshSucceeded = false;
        if (contextSearch is not null && ShouldRefreshSqlContext(sqlContext)) {
            refreshAttempted = true;
            try {
                _ = await executor.ExecuteAsync(
                    "graph-build",
                    ["-Format", "Json"],
                    cancellationToken).ConfigureAwait(false);
                fullSnapshot = await snapshots
                    .RefreshAsync(relevantPaths: null, cancellationToken)
                    .ConfigureAwait(false);
                sqlContext = await SearchSqlContextAsync(
                    contextSearch,
                    query,
                    plannedPath,
                    fullSnapshot.Fingerprint,
                    cancellationToken).ConfigureAwait(false);
                refreshSucceeded = true;
            } catch (DevelopmentMcpException exception) {
                refreshFailureReason = $"graph-refresh-{exception.ErrorCode}";
            }
        }
        bool useSqlContext = sqlContext is { Ready: true, Fresh: true, Candidates.Count: > 0 };
        string? contextUnavailableReason = useSqlContext
            ? null
            : refreshFailureReason ?? sqlContext?.UnavailableReason ??
                (contextSearch is null ? "sqlite-reader-not-configured" : "sqlite-no-candidates");
        string[] initialRelevantPaths = NormalizePaths([plannedPath]);
        List<DevelopmentContextComponentError> errors = [];
        string[] expandedScopePaths;
        if (useSqlContext) {
            expandedScopePaths = [.. new[] { plannedPath }
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Cast<string>()
                .Concat(sqlContext!.Candidates.Take(10).Select(candidate => candidate.Path))
                .Distinct(StringComparer.OrdinalIgnoreCase)];
        } else {
            errors.Add(CreateContextSearchError(contextUnavailableReason!));
            expandedScopePaths = initialRelevantPaths;
        }
        routingStopwatch.Stop();
        _telemetry.RecordContextRoute(
            useSqlContext
                ? ContextRoutingOutcome.SqlitePrimary
                : ContextRoutingOutcome.SqliteUnavailable,
            contextUnavailableReason,
            routingStopwatch.Elapsed,
            refreshAttempted,
            refreshSucceeded);
        ChangeSetSnapshot snapshot = await snapshots
            .GetAsync(expandedScopePaths, cancellationToken)
            .ConfigureAwait(false);

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
        if (useSqlContext) {
            await EnsureSnapshotUnchangedAsync(
                fullSnapshot,
                relevantPaths: null,
                cancellationToken).ConfigureAwait(false);
        }

        string[] effectiveLayers = InferLayers(snapshot.ChangedPaths.Concat(expandedScopePaths));
        return new DevelopmentContext(
            snapshot.Fingerprint,
            snapshot.GitHead,
            await briefTask.ConfigureAwait(false),
            BackendTrace: null,
            await testPlanTask.ConfigureAwait(false),
            PartialSuccess: errors.Count > 0,
            ComponentErrors: errors,
            ExpandedScopePaths: expandedScopePaths,
            ScopeMismatch: false,
            EffectiveLayers: effectiveLayers,
            CrossLayerScope: effectiveLayers.Length > 1,
            BaseRevision: baselineAvailable ? baseRevision ?? snapshot.GitHead : null,
            HeadRevision: headRevision ?? snapshot.GitHead,
            BaselineAvailable: baselineAvailable,
            SqlContextSearch: sqlContext,
            ContextRetrievalSource: useSqlContext ? "sqlite" : "unavailable",
            ContextFallbackReason: contextUnavailableReason);
    }

    private static DevelopmentContextComponentError CreateContextSearchError(string unavailableReason) {
        string recovery = unavailableReason switch {
            "sqlite-error-5" =>
                "The SQLite projection is locked by its writer. Retry after the graph writer completes.",
            "snapshot-mismatch" =>
                "The worktree changed while the projection was refreshed. Retry after the worktree stabilizes.",
            "sqlite-no-candidates" =>
                "Refine the query or provide plannedPath to continue with an explicitly bounded scope.",
            _ when unavailableReason.StartsWith("graph-refresh-", StringComparison.Ordinal) =>
                "Run ./.llm-wiki/wiki.ps1 graph-build, resolve the reported refresh failure, and retry.",
            _ =>
                "Run ./.llm-wiki/wiki.ps1 graph-build and retry, or provide plannedPath for bounded context.",
        };
        return new DevelopmentContextComponentError(
            "context-search",
            DevelopmentMcpErrorCodes.ContextSearchUnavailable,
            $"SQLite development-context search is unavailable ({unavailableReason}). {recovery} " +
            "Use trace_backend_flow explicitly only when a backend trace is required.");
    }

    private static async Task<WikiContextSearchResult?> SearchSqlContextAsync(
        IWikiContextSearch contextSearch,
        string query,
        string? plannedPath,
        string expectedChangeSetFingerprint,
        CancellationToken cancellationToken) {
        return await contextSearch.SearchAsync(
            query,
            limit: 20,
            changeType: InferSearchChangeType(plannedPath, query),
            module: null,
            scopePaths: string.IsNullOrWhiteSpace(plannedPath) ? [] : [plannedPath],
            cancellationToken,
            expectedChangeSetFingerprint).ConfigureAwait(false);
    }

    private static bool ShouldRefreshSqlContext(WikiContextSearchResult? result) =>
        result is null ||
        string.Equals(result.UnavailableReason, "database-missing", StringComparison.Ordinal) ||
        string.Equals(result.UnavailableReason, "fts-projection-not-ready", StringComparison.Ordinal) ||
        string.Equals(result.UnavailableReason, "snapshot-mismatch", StringComparison.Ordinal) ||
        string.Equals(result.UnavailableReason, "sqlite-error-11", StringComparison.Ordinal) ||
        string.Equals(result.UnavailableReason, "sqlite-error-26", StringComparison.Ordinal);

    private async Task EnsureSnapshotUnchangedAsync(
        ChangeSetSnapshot expected,
        IReadOnlyList<string>? relevantPaths,
        CancellationToken cancellationToken) {
        ChangeSetSnapshot current = await snapshots.RefreshAsync(relevantPaths, cancellationToken).ConfigureAwait(false);
        if (!string.Equals(expected.Fingerprint, current.Fingerprint, StringComparison.Ordinal)) {
            string scope = relevantPaths is { Count: > 0 }
                ? string.Join(", ", relevantPaths)
                : "complete worktree";
            throw new DevelopmentMcpException(
                DevelopmentMcpErrorCodes.SnapshotChanged,
                "The Git/worktree snapshot changed while development context was being collected. " +
                $"Scope: {scope}. " +
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

    private static string InferSearchChangeType(string? plannedPath, string query) {
        if (string.IsNullOrWhiteSpace(plannedPath)) {
            return HasTestIntent(query) ? "Tests" : "Any";
        }
        string normalized = plannedPath.Replace('\\', '/');
        if (normalized.StartsWith("FoodDiary.Web.Client/", StringComparison.OrdinalIgnoreCase)) {
            return "Frontend";
        }
        if (normalized.StartsWith("FoodDiary.Presentation.Api/", StringComparison.OrdinalIgnoreCase) ||
            normalized.StartsWith("FoodDiary.Web.Api/", StringComparison.OrdinalIgnoreCase)) {
            return "Api";
        }
        if (normalized.Contains("/Migrations/", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("/Persistence/", StringComparison.OrdinalIgnoreCase)) {
            return "Database";
        }
        if (normalized.StartsWith("tests/", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("/tests/", StringComparison.OrdinalIgnoreCase)) {
            return "Tests";
        }
        return "Backend";
    }

    private static bool HasTestIntent(string query) {
        string normalized = string.Concat(query.Select(character =>
            char.IsLetterOrDigit(character)
                ? char.ToLowerInvariant(character)
                : ' '));
        return normalized
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(term =>
                term is "test" or "tests" or "testing" or "spec" or "specs" ||
                term.StartsWith("тест", StringComparison.Ordinal));
    }
}
