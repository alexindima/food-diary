using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using FoodDiary.Development.Mcp.Infrastructure;
using FoodDiary.Development.Mcp.Protocol;
using Microsoft.Data.Sqlite;

namespace FoodDiary.Development.Mcp.Wiki;

public sealed class SqliteWikiContextSearch : IWikiContextSearch {
    private static readonly Regex CamelBoundary = new(
        @"(?<left>[\p{Ll}\p{N}])(?<right>[\p{Lu}])",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
        TimeSpan.FromMilliseconds(100));
    private static readonly Regex Separators = new(
        @"[_./\\-]+",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
        TimeSpan.FromMilliseconds(100));
    private static readonly Regex Terms = new(
        @"[\p{L}\p{N}][\p{L}\p{N}_-]*",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
        TimeSpan.FromMilliseconds(100));
    private static readonly Regex TestPath = new(
        @"(^|/)(?:tests?|[^/]+\.tests?)(/|$)|\.(?:spec|test)\.(?:ts|js|mjs|cjs)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
        TimeSpan.FromMilliseconds(100));
    private static readonly Regex InterfacePath = new(
        @"/I[A-Z][^/]*\.cs$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
        TimeSpan.FromMilliseconds(100));
    private static readonly Regex CompanionPath = new(
        @"\.[^./\\]+\.cs$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
        TimeSpan.FromMilliseconds(100));
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly string _databasePath;
    private readonly Lazy<RankingPolicy> _policy;
    private readonly WikiRuntimeTelemetry _telemetry;

    public SqliteWikiContextSearch(WikiRuntimeTelemetry telemetry)
        : this(RepositoryRootResolver.Resolve(), telemetry) {
    }

    internal SqliteWikiContextSearch(string repositoryRoot, WikiRuntimeTelemetry telemetry) {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);
        _telemetry = telemetry;
        _databasePath = Path.Combine(
            repositoryRoot,
            ".artifacts",
            "llm-wiki",
            "code-graph",
            "code-graph.sqlite");
        string policyPath = Path.Combine(
            repositoryRoot,
            ".llm-wiki",
            "policies",
            "context-search-ranking.json");
        _policy = new Lazy<RankingPolicy>(
            () => LoadPolicy(policyPath),
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public async Task<WikiContextSearchResult> SearchAsync(
        string query,
        int limit,
        string changeType,
        string? module,
        IReadOnlyList<string>? scopePaths,
        CancellationToken cancellationToken,
        string? expectedChangeSetFingerprint = null) {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        ArgumentOutOfRangeException.ThrowIfLessThan(limit, 1);
        var stopwatch = Stopwatch.StartNew();
        try {
            if (!File.Exists(_databasePath)) {
                return Unavailable("database-missing", stopwatch);
            }

            RankingPolicy policy = _policy.Value;
            string[] queryTerms = ExpandQueryTerms(query, policy);
            string[] directQueryTerms = GetDirectQueryTerms(query, policy);
            string[] rankingTerms = ExpandRankingTerms(query, policy);
            if (queryTerms.Length == 0) {
                return Unavailable("query-has-no-search-terms", stopwatch, queryTerms);
            }

            string connectionString = new SqliteConnectionStringBuilder {
                DataSource = _databasePath,
                Mode = SqliteOpenMode.ReadOnly,
                Pooling = true,
                DefaultTimeout = 2,
            }.ToString();
            SqliteConnection connection = new(connectionString);
            await using ConfiguredAsyncDisposable connectionDisposal = connection.ConfigureAwait(false);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            (string? fingerprint, string? updatedAtUtc, int indexedDocuments, string? changeSetFingerprint, string? gitHead) =
                await ReadMetadataAsync(connection, cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(fingerprint) ||
                indexedDocuments == 0 ||
                string.IsNullOrWhiteSpace(changeSetFingerprint) ||
                string.IsNullOrWhiteSpace(gitHead)) {
                return Unavailable(
                    "fts-projection-not-ready",
                    stopwatch,
                    queryTerms,
                    indexedDocuments,
                    fingerprint,
                    updatedAtUtc,
                    changeSetFingerprint,
                    gitHead);
            }
            if (!string.IsNullOrWhiteSpace(expectedChangeSetFingerprint) &&
                !string.Equals(
                    changeSetFingerprint,
                    expectedChangeSetFingerprint,
                    StringComparison.Ordinal)) {
                return Unavailable(
                    "snapshot-mismatch",
                    stopwatch,
                    queryTerms,
                    indexedDocuments,
                    fingerprint,
                    updatedAtUtc,
                    changeSetFingerprint,
                    gitHead);
            }

            int candidateLimit = Math.Min(Math.Max(limit * 8, 40), 500);
            List<RawCandidate> rawCandidates = await ReadCandidatesAsync(
                connection,
                queryTerms,
                limit,
                candidateLimit,
                cancellationToken).ConfigureAwait(false);
            WikiContextSearchCandidate[] candidates = Rank(
                rawCandidates,
                query,
                queryTerms,
                directQueryTerms,
                rankingTerms,
                limit,
                candidateLimit,
                changeType,
                module,
                scopePaths,
                policy);
            return new WikiContextSearchResult(
                Authority: "sqlite-derived",
                Reader: "in-process-microsoft-data-sqlite",
                Ready: true,
                IndexedDocuments: indexedDocuments,
                Fingerprint: fingerprint,
                UpdatedAtUtc: updatedAtUtc,
                ChangeSetFingerprint: changeSetFingerprint,
                GitHead: gitHead,
                Fresh: !string.IsNullOrWhiteSpace(expectedChangeSetFingerprint),
                QueryTerms: queryTerms,
                Candidates: candidates,
                QueryDurationMilliseconds: ElapsedMilliseconds(stopwatch));
        } catch (OperationCanceledException) {
            throw;
        } catch (SqliteException exception) {
            return Unavailable(
                $"sqlite-error-{exception.SqliteErrorCode.ToString(System.Globalization.CultureInfo.InvariantCulture)}",
                stopwatch);
        } catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException or InvalidDataException) {
            return Unavailable("context-search-configuration-unavailable", stopwatch);
        } finally {
            stopwatch.Stop();
            _telemetry.RecordCommandStage("context-search", "in-process-sqlite", stopwatch.Elapsed);
        }
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    private static async Task<(
        string? Fingerprint,
        string? UpdatedAtUtc,
        int IndexedDocuments,
        string? ChangeSetFingerprint,
        string? GitHead)> ReadMetadataAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken) {
        SqliteCommand command = connection.CreateCommand();
        await using ConfiguredAsyncDisposable commandDisposal = command.ConfigureAwait(false);
        command.CommandTimeout = 2;
        command.CommandText = """
            SELECT
                (SELECT value FROM metadata WHERE key = 'context_search_fingerprint'),
                (SELECT value FROM metadata WHERE key = 'context_search_updated_at_utc'),
                (SELECT COUNT(*) FROM context_search),
                (SELECT value FROM metadata WHERE key = 'change_set_fingerprint'),
                (SELECT value FROM metadata WHERE key = 'change_set_git_head');
            """;
        SqliteDataReader reader = await command
            .ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);
        await using ConfiguredAsyncDisposable readerDisposal = reader.ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) {
            return (null, null, 0, null, null);
        }
        return (
            reader.IsDBNull(0) ? null : reader.GetString(0),
            reader.IsDBNull(1) ? null : reader.GetString(1),
            reader.GetInt32(2),
            reader.IsDBNull(3) ? null : reader.GetString(3),
            reader.IsDBNull(4) ? null : reader.GetString(4));
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    private static async Task<List<RawCandidate>> ReadCandidatesAsync(
        SqliteConnection connection,
        IReadOnlyList<string> queryTerms,
        int limit,
        int candidateLimit,
        CancellationToken cancellationToken) {
        string match = string.Join(
            " OR ",
            queryTerms.Select(term => $"\"{term.Replace("\"", "\"\"", StringComparison.Ordinal)}\"*"));
        List<RawCandidate> candidates = [];
        {
            SqliteCommand command = connection.CreateCommand();
            await using ConfiguredAsyncDisposable commandDisposal = command.ConfigureAwait(false);
            command.CommandTimeout = 2;
            command.CommandText = """
                SELECT record_type, record_key, path, source_path,
                    COALESCE(category, ''), COALESCE(title, ''),
                    bm25(context_search, 0.0, 0.0, 6.0, 0.0, 0.0, 4.0, 1.0) lexical_rank
                FROM context_search
                WHERE context_search MATCH $match
                ORDER BY lexical_rank, path
                LIMIT $limit;
                """;
            command.Parameters.AddWithValue("$match", match);
            command.Parameters.AddWithValue("$limit", candidateLimit);
            SqliteDataReader reader = await command
                .ExecuteReaderAsync(cancellationToken)
                .ConfigureAwait(false);
            await using ConfiguredAsyncDisposable readerDisposal = reader.ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) {
                candidates.Add(new RawCandidate(
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetString(3),
                    reader.GetString(4),
                    reader.GetString(5),
                    reader.GetDouble(6)));
            }
        }

        string identityMatch = string.Join(
            " OR ",
            queryTerms.SelectMany(term => {
                string escaped = term.Replace("\"", "\"\"", StringComparison.Ordinal);
                return new[] { $"path : \"{escaped}\"*", $"title : \"{escaped}\"*" };
            }));
        int identityLimit = Math.Min(Math.Max(limit * 2, 20), 100);
        {
            SqliteCommand command = connection.CreateCommand();
            await using ConfiguredAsyncDisposable commandDisposal = command.ConfigureAwait(false);
            command.CommandTimeout = 2;
            command.CommandText = """
                SELECT record_type, record_key, path, source_path,
                    COALESCE(category, ''), COALESCE(title, ''),
                    bm25(context_search, 0.0, 0.0, 6.0, 0.0, 0.0, 4.0, 1.0) lexical_rank
                FROM context_search
                WHERE context_search MATCH $match
                ORDER BY lexical_rank, path
                LIMIT $limit;
                """;
            command.Parameters.AddWithValue("$match", identityMatch);
            command.Parameters.AddWithValue("$limit", identityLimit);
            SqliteDataReader reader = await command
                .ExecuteReaderAsync(cancellationToken)
                .ConfigureAwait(false);
            await using ConfiguredAsyncDisposable readerDisposal = reader.ConfigureAwait(false);
            var candidateIndexes = candidates
                .Select((candidate, index) => new { Key = CandidateKey(candidate), Index = index })
                .ToDictionary(item => item.Key, item => item.Index, StringComparer.Ordinal);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) {
                RawCandidate candidate = new(
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetString(3),
                    reader.GetString(4),
                    reader.GetString(5),
                    reader.GetDouble(6));
                string key = CandidateKey(candidate);
                if (candidateIndexes.TryAdd(key, candidates.Count)) {
                    candidates.Add(candidate);
                }
            }
        }
        return candidates;
    }

    private static string CandidateKey(RawCandidate candidate) =>
        $"{candidate.RecordType}\0{candidate.RecordKey}\0{candidate.Path}";

    private static WikiContextSearchCandidate[] Rank(
        IReadOnlyList<RawCandidate> candidates,
        string query,
        IReadOnlyList<string> queryTerms,
        IReadOnlyList<string> directQueryTerms,
        IReadOnlyList<string> rankingTerms,
        int limit,
        int candidateLimit,
        string changeType,
        string? module,
        IReadOnlyList<string>? scopePaths,
        RankingPolicy policy) {
        string normalizedQuery = ExpandSearchText(query).ToLowerInvariant();
        string moduleTerm = module?.ToLowerInvariant() ?? string.Empty;
        string[] normalizedScopes = [.. (scopePaths ?? [])
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => NormalizePath(path).ToLowerInvariant())];
        HashSet<string> terms = new(rankingTerms, StringComparer.Ordinal);
        HashSet<string> directTerms = new(directQueryTerms, StringComparer.Ordinal);
        List<RankedCandidate> ranked = [];
        for (int index = 0; index < candidates.Count; index++) {
            RawCandidate candidate = candidates[index];
            string normalizedPath = NormalizePath(candidate.Path).ToLowerInvariant();
            bool isTest = TestPath.IsMatch(candidate.Path);
            string normalizedTitle = ExpandSearchText(candidate.Title).ToLowerInvariant();
            List<string> reasons = ["SQLite FTS5 lexical match"];
            int score = candidateLimit - index;
            if (normalizedPath.Contains(normalizedQuery, StringComparison.Ordinal) ||
                normalizedTitle.Contains(normalizedQuery, StringComparison.Ordinal)) {
                score += 80;
                reasons.Add("exact normalized query match");
            }
            if (moduleTerm.Length > 0 && normalizedPath.Contains(moduleTerm, StringComparison.Ordinal)) {
                score += 50;
                reasons.Add($"module {module}");
            }
            if (normalizedScopes.Any(scope =>
                normalizedPath.Equals(scope, StringComparison.Ordinal) ||
                normalizedPath.StartsWith($"{scope}/", StringComparison.Ordinal) ||
                scope.StartsWith($"{normalizedPath}/", StringComparison.Ordinal))) {
                score += 70;
                reasons.Add("planned scope affinity");
            }
            string searchablePath = ExpandSearchText(NormalizePath(candidate.Path)).ToLowerInvariant();
            string searchableIdentity = $"{searchablePath} {normalizedTitle}";
            string searchableFileIdentity =
                ExpandSearchText(Path.GetFileName(candidate.Path)).ToLowerInvariant();
            bool matchedRankingPolicy = false;
            string[] identityMatches = [.. queryTerms
                .Where(term =>
                    term.Length >= policy.PathTermAffinity.MinimumTermLength &&
                    searchableIdentity.Contains(term, StringComparison.Ordinal))
                .Distinct(StringComparer.Ordinal)];
            int identityScore = Math.Min(
                identityMatches.Length * policy.PathTermAffinity.ScorePerMatch,
                policy.PathTermAffinity.MaximumScore);
            if (identityScore > 0) {
                score += identityScore;
                reasons.Add($"path/title affinity {string.Join(", ", identityMatches)}");
            }
            foreach (IdentityBoost boost in policy.IdentityBoosts) {
                bool matchesChangeType = boost.ChangeTypes is null ||
                    boost.ChangeTypes.Length == 0 ||
                    boost.ChangeTypes.Any(candidateChangeType =>
                        string.Equals(candidateChangeType, changeType, StringComparison.OrdinalIgnoreCase));
                if (!matchesChangeType) {
                    continue;
                }
                HashSet<string> eligibleQueryTerms = boost.DirectOnly ? directTerms : terms;
                string eligibleIdentity = string.Equals(
                    boost.IdentityScope,
                    "file",
                    StringComparison.OrdinalIgnoreCase)
                    ? searchableFileIdentity
                    : searchablePath;
                int queryMatches = boost.QueryTerms.Count(term =>
                    eligibleQueryTerms.Contains(term.ToLowerInvariant()));
                int identityMatchesBoost = boost.IdentityTerms.Count(term =>
                    eligibleIdentity.Contains(term.ToLowerInvariant(), StringComparison.Ordinal));
                if (queryMatches >= boost.MinimumMatches &&
                    identityMatchesBoost >= Math.Max(1, boost.MinimumIdentityMatches)) {
                    score += boost.Score;
                    matchedRankingPolicy |= string.Equals(
                        boost.IdentityScope,
                        "file",
                        StringComparison.OrdinalIgnoreCase);
                    reasons.Add($"ranking policy {boost.Id}");
                }
            }
            foreach (StructuralRoleBoost boost in policy.StructuralRoleBoosts ?? []) {
                bool matchesChangeType = boost.ChangeTypes is null ||
                    boost.ChangeTypes.Length == 0 ||
                    boost.ChangeTypes.Any(candidateChangeType =>
                        string.Equals(candidateChangeType, changeType, StringComparison.OrdinalIgnoreCase));
                if (!matchesChangeType || (boost.ExcludeTests && isTest) ||
                    (boost.RecordTypes is { Length: > 0 } && !boost.RecordTypes.Any(recordType =>
                        string.Equals(recordType, candidate.RecordType, StringComparison.OrdinalIgnoreCase))) ||
                    (boost.PathPrefixes is { Length: > 0 } && !boost.PathPrefixes.Any(prefix =>
                        normalizedPath.StartsWith(NormalizePath(prefix).ToLowerInvariant(), StringComparison.Ordinal))) ||
                    (boost.ExcludedPathPrefixes is { Length: > 0 } && boost.ExcludedPathPrefixes.Any(prefix =>
                        normalizedPath.StartsWith(NormalizePath(prefix).ToLowerInvariant(), StringComparison.Ordinal))) ||
                    (boost.PathSuffixes is { Length: > 0 } && !boost.PathSuffixes.Any(suffix =>
                        normalizedPath.EndsWith(suffix.ToLowerInvariant(), StringComparison.Ordinal)))) {
                    continue;
                }
                HashSet<string> eligibleQueryTerms = boost.DirectOnly ? directTerms : terms;
                if (boost.ExcludedQueryTerms?.Any(term =>
                    eligibleQueryTerms.Contains(term.ToLowerInvariant())) == true) {
                    continue;
                }
                int queryMatches = boost.QueryTerms?.Count(term =>
                    eligibleQueryTerms.Contains(term.ToLowerInvariant())) ?? 0;
                string eligibleIdentity = boost.IdentityScope?.ToLowerInvariant() switch {
                    "file" => searchableFileIdentity,
                    "identity" => searchableIdentity,
                    _ => searchablePath,
                };
                int candidateMatches = boost.CandidateTerms?.Count(term =>
                    eligibleIdentity.Contains(term.ToLowerInvariant(), StringComparison.Ordinal)) ?? 0;
                int minimumAffinityTermLength = boost.MinimumAffinityTermLength ??
                    policy.PathTermAffinity.MinimumTermLength;
                HashSet<string> affinityQueryTerms = boost.AffinityDirectOnly ? directTerms : terms;
                string[] queryIdentityMatches = [.. affinityQueryTerms.Where(term =>
                    term.Length >= minimumAffinityTermLength &&
                    eligibleIdentity.Contains(term, StringComparison.Ordinal))];
                if (queryMatches < boost.MinimumMatches ||
                    candidateMatches < boost.MinimumCandidateMatches ||
                    queryIdentityMatches.Length < boost.MinimumQueryIdentityMatches) {
                    continue;
                }
                int variableScore = Math.Min(
                    queryIdentityMatches.Length * boost.ScorePerQueryIdentityMatch,
                    boost.MaximumQueryIdentityScore ?? int.MaxValue);
                score += boost.Score + variableScore;
                matchedRankingPolicy = true;
                reasons.Add($"structural role {boost.Id} ({string.Join(", ", queryIdentityMatches)})");
            }
            foreach (PathBoost boost in policy.PathBoosts) {
                HashSet<string> eligibleQueryTerms = boost.DirectOnly ? directTerms : terms;
                int matchedTerms = boost.QueryTerms.Count(term =>
                    eligibleQueryTerms.Contains(term.ToLowerInvariant()));
                bool matchesPath = boost.PathPrefixes.Any(prefix =>
                    normalizedPath.StartsWith(NormalizePath(prefix).ToLowerInvariant(), StringComparison.Ordinal));
                if (matchedTerms >= boost.MinimumMatches && matchesPath) {
                    score += boost.Score;
                    matchedRankingPolicy = true;
                    reasons.Add($"ranking policy {boost.Id}");
                }
            }
            if (matchedRankingPolicy) {
                string[] fileNameMatches = [.. rankingTerms
                    .Where(term =>
                        term.Length >= policy.MatchedPolicyFileNameAffinity.MinimumTermLength &&
                        searchableFileIdentity.Contains(term, StringComparison.Ordinal))
                    .Distinct(StringComparer.Ordinal)];
                int roleAffinityScore = Math.Min(
                    fileNameMatches.Length * policy.MatchedPolicyFileNameAffinity.ScorePerMatch,
                    policy.MatchedPolicyFileNameAffinity.MaximumScore);
                if (roleAffinityScore > 0) {
                    score += roleAffinityScore;
                    reasons.Add($"matched-role file-name affinity {string.Join(", ", fileNameMatches)}");
                }
            }
            if (isTest && !string.Equals(changeType, "Tests", StringComparison.OrdinalIgnoreCase)) {
                score -= policy.NonTestPenalty;
            }
            bool isFrontendPath = normalizedPath.StartsWith(
                "fooddiary.web.client/",
                StringComparison.Ordinal);
            bool isCode = string.Equals(candidate.RecordType, "code", StringComparison.Ordinal);
            if (isCode &&
                string.Equals(changeType, "Frontend", StringComparison.OrdinalIgnoreCase) &&
                !isFrontendPath) {
                score -= policy.CrossLayerPenalty;
                reasons.Add("backend candidate penalty for frontend intent");
            } else if (isFrontendPath &&
                (string.Equals(changeType, "Api", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(changeType, "Backend", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(changeType, "Database", StringComparison.OrdinalIgnoreCase))) {
                score -= policy.CrossLayerPenalty;
                reasons.Add("frontend candidate penalty for backend intent");
            }
            bool requestsAbstraction = terms.Overlaps(["interface", "contract", "abstraction"]);
            if (!requestsAbstraction && normalizedPath.StartsWith(
                "fooddiary.application.abstractions/",
                StringComparison.Ordinal)) {
                score -= policy.ApplicationAbstractionPenalty;
            }
            if (!requestsAbstraction && InterfacePath.IsMatch(candidate.Path)) {
                score -= policy.InterfacePathPenalty;
            }
            if (CompanionPath.IsMatch(candidate.Path)) {
                score -= policy.CompanionFilePenalty;
                reasons.Add("companion file ranked after primary declaration");
            }
            if (isCode) {
                score += 20;
            }
            if (string.Equals(candidate.RecordType, "agent-guide", StringComparison.Ordinal)) {
                score += policy.AgentGuideBoost;
            }
            ranked.Add(new RankedCandidate(candidate, score, reasons));
        }

        HashSet<string> seenPaths = new(StringComparer.OrdinalIgnoreCase);
        List<WikiContextSearchCandidate> result = [];
        foreach (RankedCandidate candidate in ranked
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Raw.LexicalRank)
            .ThenBy(item => item.Raw.Path, StringComparer.Ordinal)) {
            if (!seenPaths.Add(candidate.Raw.Path)) {
                continue;
            }
            result.Add(new WikiContextSearchCandidate(
                result.Count + 1,
                candidate.Raw.Path,
                candidate.Raw.RecordType,
                candidate.Raw.Category,
                candidate.Score,
                Math.Round(candidate.Raw.LexicalRank, 6, MidpointRounding.AwayFromZero),
                candidate.Reasons));
            if (result.Count >= limit) {
                break;
            }
        }
        return [.. result];
    }

    private static string[] ExpandQueryTerms(string query, RankingPolicy policy) {
        string[] directTerms = GetDirectQueryTerms(query, policy);
        List<string> terms = [.. directTerms];
        HashSet<string> seen = new(terms, StringComparer.Ordinal);
        foreach (string term in directTerms) {
            AddExpansions(GetEnglishMorphologicalVariants(term), terms, seen);
        }
        AddConfiguredExpansions(directTerms, policy, terms, seen);
        return [.. terms.Take(policy.MaximumQueryTerms)];
    }

    private static string[] ExpandRankingTerms(string query, RankingPolicy policy) {
        string[] directTerms = GetDirectQueryTerms(query, policy);
        List<string> terms = [.. directTerms];
        HashSet<string> seen = new(terms, StringComparer.Ordinal);
        AddConfiguredExpansions(directTerms, policy, terms, seen);
        return [.. terms.Take(policy.MaximumQueryTerms)];
    }

    private static string[] GetDirectQueryTerms(string query, RankingPolicy policy) {
        List<string> terms = [];
        HashSet<string> seen = new(StringComparer.Ordinal);
        HashSet<string> stopTerms = new(policy.StopTerms, StringComparer.Ordinal);
        foreach (Match match in Terms.Matches(ExpandSearchText(query).ToLowerInvariant())) {
            if (match.Value.Length >= 2 && !stopTerms.Contains(match.Value) && seen.Add(match.Value)) {
                terms.Add(match.Value);
            }
        }
        return [.. terms];
    }

    private static void AddConfiguredExpansions(
        IReadOnlyList<string> directTerms,
        RankingPolicy policy,
        List<string> terms,
        HashSet<string> seen) {
        foreach (string term in directTerms) {
            if (policy.QueryTermExpansions.TryGetValue(term, out string[]? expansions)) {
                AddExpansions(expansions, terms, seen);
            }
            foreach ((string prefix, string[] prefixExpansions) in policy.QueryPrefixExpansions) {
                if (term.StartsWith(prefix, StringComparison.Ordinal)) {
                    AddExpansions(prefixExpansions, terms, seen);
                }
            }
        }
    }

    private static string[] GetEnglishMorphologicalVariants(string term) {
        if (term.Any(character => character is < 'a' or > 'z')) {
            return [];
        }
        List<string> variants = [];
        if (term.Length > 4 && term.EndsWith("ies", StringComparison.Ordinal)) {
            variants.Add($"{term[..^3]}y");
        } else if (term.Length > 3 && term.EndsWith('s') && !term.EndsWith("ss", StringComparison.Ordinal)) {
            variants.Add(term[..^1]);
        }
        if (term.Length > 5 && term.EndsWith("ing", StringComparison.Ordinal)) {
            string stem = term[..^3];
            variants.Add(stem);
            variants.Add($"{stem}e");
            if (stem.Length > 2 && stem[^1] == stem[^2]) {
                variants.Add(stem[..^1]);
            }
        }
        if (term.Length > 4 && term.EndsWith("ed", StringComparison.Ordinal)) {
            variants.Add(term[..^2]);
            variants.Add(term[..^1]);
        }
        return [.. variants];
    }

    private static void AddExpansions(
        IReadOnlyList<string> expansions,
        List<string> terms,
        HashSet<string> seen) {
        foreach (string expansion in expansions) {
            if (seen.Add(expansion)) {
                terms.Add(expansion);
            }
        }
    }

    private static string ExpandSearchText(string value) {
        string expanded = Separators.Replace(
            CamelBoundary.Replace(value, "${left} ${right}"),
            " ");
        return string.Equals(expanded, value, StringComparison.Ordinal)
            ? value
            : $"{value} {expanded}";
    }

    private static RankingPolicy LoadPolicy(string path) {
        if (!File.Exists(path)) {
            throw new InvalidDataException("Context-search ranking policy is missing.");
        }
        RankingPolicy? policy = JsonSerializer.Deserialize<RankingPolicy>(
            File.ReadAllText(path),
            JsonOptions);
        if (policy is null ||
            policy.SchemaVersion != 1 ||
            policy.MaximumQueryTerms < 1 ||
            policy.StopTerms is null ||
            policy.PathTermAffinity is null ||
            policy.MatchedPolicyFileNameAffinity is null ||
            policy.QueryTermExpansions is null ||
            policy.QueryPrefixExpansions is null ||
            policy.PathBoosts is null ||
            policy.IdentityBoosts is null ||
            policy.IdentityBoosts?.Any(boost =>
                !string.IsNullOrWhiteSpace(boost.IdentityScope) &&
                !string.Equals(boost.IdentityScope, "path", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(boost.IdentityScope, "file", StringComparison.OrdinalIgnoreCase)) == true ||
            policy.StructuralRoleBoosts?.Any(boost =>
                !string.IsNullOrWhiteSpace(boost.IdentityScope) &&
                !string.Equals(boost.IdentityScope, "path", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(boost.IdentityScope, "file", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(boost.IdentityScope, "identity", StringComparison.OrdinalIgnoreCase)) == true) {
            throw new InvalidDataException("Context-search ranking policy is invalid.");
        }
        return policy;
    }

    private static WikiContextSearchResult Unavailable(
        string reason,
        Stopwatch stopwatch,
        IReadOnlyList<string>? queryTerms = null,
        int indexedDocuments = 0,
        string? fingerprint = null,
        string? updatedAtUtc = null,
        string? changeSetFingerprint = null,
        string? gitHead = null) =>
        new(
            Authority: "sqlite-derived",
            Reader: "in-process-microsoft-data-sqlite",
            Ready: false,
            IndexedDocuments: indexedDocuments,
            Fingerprint: fingerprint,
            UpdatedAtUtc: updatedAtUtc,
            ChangeSetFingerprint: changeSetFingerprint,
            GitHead: gitHead,
            Fresh: false,
            QueryTerms: queryTerms ?? [],
            Candidates: [],
            QueryDurationMilliseconds: ElapsedMilliseconds(stopwatch),
            UnavailableReason: reason);

    private static double ElapsedMilliseconds(Stopwatch stopwatch) =>
        Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2, MidpointRounding.AwayFromZero);

    private static string NormalizePath(string path) => path.Replace('\\', '/');

    private sealed record RawCandidate(
        string RecordType,
        string RecordKey,
        string Path,
        string SourcePath,
        string Category,
        string Title,
        double LexicalRank);

    private sealed record RankedCandidate(
        RawCandidate Raw,
        int Score,
        IReadOnlyList<string> Reasons);

    private sealed record RankingPolicy(
        int SchemaVersion,
        int MaximumQueryTerms,
        string[] StopTerms,
        PathTermAffinity PathTermAffinity,
        PathTermAffinity MatchedPolicyFileNameAffinity,
        int NonTestPenalty,
        int ApplicationAbstractionPenalty,
        int InterfacePathPenalty,
        int AgentGuideBoost,
        int CompanionFilePenalty,
        int CrossLayerPenalty,
        Dictionary<string, string[]> QueryTermExpansions,
        Dictionary<string, string[]> QueryPrefixExpansions,
        PathBoost[] PathBoosts,
        IdentityBoost[] IdentityBoosts,
        StructuralRoleBoost[]? StructuralRoleBoosts = null);

    private sealed record PathTermAffinity(
        int MinimumTermLength,
        int ScorePerMatch,
        int MaximumScore);

    private sealed record PathBoost(
        string Id,
        string[] QueryTerms,
        int MinimumMatches,
        string[] PathPrefixes,
        int Score,
        bool DirectOnly = false);

    private sealed record IdentityBoost(
        string Id,
        string[] QueryTerms,
        int MinimumMatches,
        string[] IdentityTerms,
        int MinimumIdentityMatches,
        int Score,
        bool DirectOnly = false,
        string? IdentityScope = null,
        string[]? ChangeTypes = null);

    private sealed record StructuralRoleBoost(
        string Id,
        string[]? QueryTerms = null,
        int MinimumMatches = 0,
        string[]? CandidateTerms = null,
        int MinimumCandidateMatches = 0,
        int MinimumQueryIdentityMatches = 0,
        int Score = 0,
        int ScorePerQueryIdentityMatch = 0,
        int? MaximumQueryIdentityScore = null,
        int? MinimumAffinityTermLength = null,
        bool DirectOnly = false,
        string? IdentityScope = null,
        string[]? ChangeTypes = null,
        string[]? RecordTypes = null,
        string[]? PathPrefixes = null,
        string[]? ExcludedPathPrefixes = null,
        string[]? PathSuffixes = null,
        string[]? ExcludedQueryTerms = null,
        bool AffinityDirectOnly = true,
        bool ExcludeTests = false);
}
