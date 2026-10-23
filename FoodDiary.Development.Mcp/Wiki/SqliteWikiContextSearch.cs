using System.Text.RegularExpressions;
using System.Diagnostics;
using FoodDiary.Development.Mcp.Infrastructure;
using FoodDiary.Development.Mcp.Protocol;

namespace FoodDiary.Development.Mcp.Wiki;

public sealed class SqliteWikiContextSearch : IWikiContextSearch, IWikiFlowScopeSearch {
    private readonly SqliteContextSearchReader _reader;
    private readonly SqliteFlowScopeReader _flowReader;
    private readonly WikiRuntimeTelemetry _telemetry;

    public SqliteWikiContextSearch(WikiRuntimeTelemetry telemetry)
        : this(RepositoryRootResolver.Resolve(), telemetry) {
    }

    internal SqliteWikiContextSearch(string repositoryRoot, WikiRuntimeTelemetry telemetry) {
        _reader = new SqliteContextSearchReader(repositoryRoot);
        _flowReader = new SqliteFlowScopeReader(repositoryRoot);
        _telemetry = telemetry;
    }

    public async Task<DevelopmentFlowScope> ExpandFlowScopeAsync(
        string query,
        IReadOnlyList<string> seedPaths,
        IReadOnlyList<string> queryTerms,
        bool includeTests,
        string expectedChangeSetFingerprint,
        CancellationToken cancellationToken) {
        var stopwatch = Stopwatch.StartNew();
        List<string> focusedBackend = [];
        List<string> focusedFrontend = [];
        string[] anchors = [.. GetFlowAnchors(seedPaths, queryTerms)];
        string operations = GetFlowOperations(query);
        List<(string Query, int Limit, string ChangeType)> requests = [];
        foreach (string anchor in anchors) {
            requests.Add((anchor + " " + operations + " handler", 6, "Backend"));
            requests.Add((anchor + " service", 4, "Frontend"));
        }
        if (includeTests) { requests.Add((query, 5, "Tests")); }
        IReadOnlyList<WikiContextSearchResult> results = await SearchBatchAsync(requests, cancellationToken,
            expectedChangeSetFingerprint).ConfigureAwait(false);
        if (results.Any(result => !result.Ready || !result.Fresh)) {
            return new DevelopmentFlowScope(Ready: false, seedPaths, [], [], Truncated: false,
                stopwatch.Elapsed.TotalMilliseconds, UnavailableReason: "anchor-search-unavailable");
        }
        for (int index = 0; index < anchors.Length; index++) {
            string anchor = anchors[index];
            WikiContextSearchResult handlers = results[index * 2];
            WikiContextSearchResult clients = results[(index * 2) + 1];
            focusedBackend.AddRange(handlers.Candidates.Select(candidate => candidate.Path)
                .Where(path => anchor.Split(' ').All(term => path.Contains(term, StringComparison.OrdinalIgnoreCase))));
            focusedFrontend.AddRange(clients.Candidates.Select(candidate => candidate.Path)
                .Where(path => string.Equals(SqliteFlowScopeReader.Stage(path), "frontend", StringComparison.Ordinal) &&
                    anchor.Split(' ').All(term => path.Contains(term, StringComparison.OrdinalIgnoreCase))));
        }
        string[] backendPaths = [.. focusedBackend
            .Where(path => SqliteFlowScopeReader.Stage(path) is "handler" or "service" or "worker" or "endpoint" or "infrastructure")
            .Distinct(StringComparer.OrdinalIgnoreCase)];
        string[] graphSeeds = [.. backendPaths.Where(path => string.Equals(SqliteFlowScopeReader.Stage(path), "handler", StringComparison.Ordinal)).Take(4)
            .Concat(seedPaths.Take(2)).Concat(focusedFrontend.Take(2))
            .Distinct(StringComparer.OrdinalIgnoreCase)];
        DevelopmentFlowScope graph = await _flowReader.ExpandAsync(graphSeeds, includeTests,
            expectedChangeSetFingerprint, cancellationToken).ConfigureAwait(false);
        if (!graph.Ready) { return graph; }
        string[] relatedPaths = [.. graph.Paths.Where(path => anchors.SelectMany(anchor => anchor.Split(' '))
                .Any(term => path.Contains(term, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(path => anchors.SelectMany(anchor => anchor.Split(' '))
            .Count(term => path.Contains(term, StringComparison.OrdinalIgnoreCase)))];
        _telemetry.RecordCommandStage("context-search", "flow-graph", TimeSpan.FromMilliseconds(graph.DurationMilliseconds));
        WikiContextSearchResult? tests = includeTests ? results[^1] : null;
        string[] testPaths = [.. (tests?.Candidates ?? []).Select(candidate => candidate.Path)
            .Where(path => string.Equals(SqliteFlowScopeReader.Stage(path), "tests", StringComparison.Ordinal))];
        string[] pool = [.. seedPaths.Concat(backendPaths).Concat(focusedFrontend).Concat(relatedPaths).Concat(testPaths)
            .Distinct(StringComparer.OrdinalIgnoreCase)];
        List<string> selected = [.. seedPaths.Where(path => !path.Contains("/generated/", StringComparison.OrdinalIgnoreCase) &&
            !path.Contains("/fooddiary-admin/", StringComparison.OrdinalIgnoreCase)).Take(3)];
        string[] expectedStages = includeTests
            ? ["frontend", "endpoint", "handler", "service", "infrastructure", "tests"]
            : ["frontend", "endpoint", "handler", "infrastructure"];
        bool requestsWorker = Regex.IsMatch(query,
            @"\b(?:worker|job|background)\b|задан|фонов|воркер",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(100));
        if (requestsWorker) { expectedStages = [.. expectedStages, "worker"]; }
        foreach (string stage in expectedStages) {
            if (selected.Any(path => string.Equals(SqliteFlowScopeReader.Stage(path), stage, StringComparison.Ordinal))) { continue; }
            string? path = (string.Equals(stage, "tests", StringComparison.Ordinal) ? testPaths : pool).FirstOrDefault(path => string.Equals(SqliteFlowScopeReader.Stage(path), stage, StringComparison.Ordinal));
            if (path is not null) { selected.Add(path); }
        }
        // Detailed multi-stage requests get room for intermediate commands and
        // workers; ordinary client/server navigation retains its ten-path budget.
        int limit = includeTests || requestsWorker ? 12 : 10;
        string[] paths = [.. selected.Concat(backendPaths.Take(6)).Concat(relatedPaths.Where(path => SqliteFlowScopeReader.Stage(path) is "handler" or "service").Take(5))
            .Concat(focusedFrontend.Take(3)).Concat(seedPaths).Concat(relatedPaths)
            .Distinct(StringComparer.OrdinalIgnoreCase).Take(limit)];
        DevelopmentFlowLink[] evidence = [.. graph.Links.Where(link => paths.Contains(link.TargetPath, StringComparer.OrdinalIgnoreCase))];
        return new DevelopmentFlowScope(Ready: true, paths, evidence,
            [.. expectedStages.Where(stage => !paths.Any(path => string.Equals(SqliteFlowScopeReader.Stage(path), stage, StringComparison.Ordinal)))],
            graph.Truncated || pool.Length > paths.Length, stopwatch.Elapsed.TotalMilliseconds);
    }

    private static IEnumerable<string> GetFlowAnchors(IReadOnlyList<string> paths, IReadOnlyList<string> queryTerms) {
        HashSet<string> roles = new(StringComparer.OrdinalIgnoreCase) {
            "service", "services", "handler", "command", "query", "form", "manage", "calculator", "read", "write",
            "store", "repository", "model", "configuration", "job", "image", "statistics", "controller", "result", "response",
            "manual", "dialog", "item", "items", "list", "page", "history", "data", "snapshot", "summary", "detail", "sidebar", "editor",
        };
        return paths.Where(path => !path.Contains("/generated/", StringComparison.OrdinalIgnoreCase) &&
                !path.Contains("/fooddiary-admin/", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(SqliteFlowScopeReader.Stage(path), "tests", StringComparison.Ordinal))
            .Select(path => Regex.Replace(Path.GetFileName(path).Split('.')[0],
                @"(?<=[a-z0-9])(?=[A-Z])|[-_]", " ", RegexOptions.CultureInvariant,
                TimeSpan.FromMilliseconds(100)))
            .Select(name => string.Join(' ', name.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(term => !roles.Contains(term)).Take(2)))
            .Where(anchor => anchor.Length >= 4).Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(anchor => (anchor.Split(' ').Count(term => queryTerms.Contains(term, StringComparer.OrdinalIgnoreCase)) * 100) +
                paths.Count(path => anchor.Split(' ').Any(term => path.Contains(term, StringComparison.OrdinalIgnoreCase))))
            .Take(1);
    }

    private static string GetFlowOperations(string query) {
        string value = query.ToLowerInvariant();
        if (Regex.IsMatch(value, @"\b(?:chang(?:e|es|ing|ed)|updat\w*)\b|измен|обнов",
            RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100))) { return "update"; }
        if (Regex.IsMatch(value, @"\bstart\w*\b|запуск|запуст",
            RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100))) { return "start process"; }
        return "";
    }

    public async Task<WikiContextSearchResult> SearchAsync(
        string query,
        int limit,
        string changeType,
        string? module,
        IReadOnlyList<string>? scopePaths,
        CancellationToken cancellationToken,
        string? expectedChangeSetFingerprint = null) {
        var stopwatch = Stopwatch.StartNew();
        try {
            return await _reader.SearchAsync(query, limit, changeType, module, scopePaths,
                cancellationToken, expectedChangeSetFingerprint).ConfigureAwait(false);
        } finally {
            _telemetry.RecordCommandStage("context-search", "in-process-sqlite", stopwatch.Elapsed);
        }
    }

    internal async Task<IReadOnlyList<WikiContextSearchResult>> SearchBatchAsync(
        IReadOnlyList<(string Query, int Limit, string ChangeType)> requests,
        CancellationToken cancellationToken,
        string? expectedChangeSetFingerprint) {
        var stopwatch = Stopwatch.StartNew();
        try {
            return await _reader.SearchBatchAsync(requests, cancellationToken, expectedChangeSetFingerprint).ConfigureAwait(false);
        } finally {
            _telemetry.RecordCommandStage("context-search", "in-process-sqlite-batch", stopwatch.Elapsed);
        }
    }
}
