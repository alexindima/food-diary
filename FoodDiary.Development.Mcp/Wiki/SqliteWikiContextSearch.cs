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
    private static readonly Regex HyphenatedIdentifier = new(
        @"\p{L}{2,}(?:-\p{L}{2,})+",
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
    private static readonly Regex McpIntent = new(
        @"(^|\W)mcp(\W|$)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
        TimeSpan.FromMilliseconds(100));
    private static readonly Regex ExplicitIdentifier = new(
        @"\b[A-Za-z][A-Za-z0-9_]{5,}\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
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
            IReadOnlyList<HashSet<string>> negativeRoleGroups = GetNegatedRoleTermGroups(query, policy);
            HashSet<string> negativeRoleTerms = new(
                negativeRoleGroups.SelectMany(group => group),
                StringComparer.Ordinal);
            string[] alternativeTerms = GetNegatedRoleAlternatives(negativeRoleGroups, policy);
            string[] queryTerms = [.. ExpandQueryTerms(query, policy)
                .Where(term => !negativeRoleTerms.Contains(term))
                .Concat(alternativeTerms)
                .Distinct(StringComparer.Ordinal)
                .Take(policy.MaximumQueryTerms)];
            string[] directQueryTerms = [.. GetDirectQueryTerms(query, policy)
                .Where(term => !negativeRoleTerms.Contains(term))];
            string[] rankingTerms = [.. ExpandRankingTerms(query, policy)
                .Where(term => !negativeRoleTerms.Contains(term))
                .Concat(alternativeTerms)
                .Distinct(StringComparer.Ordinal)
                .Take(policy.MaximumQueryTerms)];
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

            int candidateLimit = policy.CandidatePoolLimit;
            List<RawCandidate> rawCandidates = await ReadCandidatesAsync(
                connection,
                query,
                queryTerms,
                directQueryTerms,
                candidateLimit,
                policy.IdentityCandidatePoolLimit,
                cancellationToken).ConfigureAwait(false);
            WikiContextSearchCandidate[] candidates = Rank(
                rawCandidates,
                query,
                queryTerms,
                directQueryTerms,
                rankingTerms,
                negativeRoleGroups,
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
        string query,
        IReadOnlyList<string> queryTerms,
        IReadOnlyList<string> directQueryTerms,
        int candidateLimit,
        int identityCandidateLimit,
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
            command.Parameters.AddWithValue("$limit", identityCandidateLimit);
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
        {
            SqliteCommand command = connection.CreateCommand();
            await using ConfiguredAsyncDisposable commandDisposal = command.ConfigureAwait(false);
            command.CommandTimeout = 2;
            command.CommandText = """
                SELECT search.record_type, search.record_key, search.path, search.source_path,
                    COALESCE(search.category, ''), COALESCE(search.title, ''),
                    bm25(context_search_identity, 6.0, 4.0) lexical_rank
                FROM context_search_identity
                JOIN context_search search ON search.rowid = context_search_identity.rowid
                WHERE context_search_identity MATCH $match
                ORDER BY lexical_rank, search.path, search.rowid
                LIMIT $limit;
                """;
            command.Parameters.AddWithValue("$match", identityMatch);
            command.Parameters.AddWithValue("$limit", identityCandidateLimit);
            SqliteDataReader reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            await using ConfiguredAsyncDisposable readerDisposal = reader.ConfigureAwait(false);
            var candidateKeys = candidates.Select(CandidateKey).ToHashSet(StringComparer.Ordinal);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) {
                RawCandidate candidate = new(
                    reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
                    reader.GetString(4), reader.GetString(5), reader.GetDouble(6));
                if (candidateKeys.Add(CandidateKey(candidate))) {
                    candidates.Add(candidate);
                }
            }
        }
        string[] runtimeSuffixes = [];
        if (directQueryTerms.Any(term => term is "node" or "javascript" or "mjs")) {
            runtimeSuffixes = [".mjs", ".js", ".cjs"];
        } else if (!McpIntent.IsMatch(query) &&
            directQueryTerms.Any(term => term is "powershell" or "pwsh" or "ps1")) {
            runtimeSuffixes = [".ps1"];
        }
        if (runtimeSuffixes.Length > 0) {
            HashSet<string> runtimeCandidateKeys = new(StringComparer.Ordinal);
            List<RawCandidate> runtimeCandidatesToPrepend = [];
            foreach (string suffix in runtimeSuffixes) {
                SqliteCommand command = connection.CreateCommand();
                await using ConfiguredAsyncDisposable commandDisposal = command.ConfigureAwait(false);
                command.CommandTimeout = 2;
                command.CommandText = """
                    SELECT record_type, record_key, path, source_path,
                        COALESCE(category, ''), COALESCE(title, ''), 0.0 lexical_rank
                    FROM context_search
                    WHERE context_search.path GLOB $suffix
                    ORDER BY context_search.path;
                    """;
                command.Parameters.AddWithValue("$suffix", $"*{suffix}");
                SqliteDataReader reader = await command
                    .ExecuteReaderAsync(cancellationToken)
                    .ConfigureAwait(false);
                await using ConfiguredAsyncDisposable readerDisposal = reader.ConfigureAwait(false);
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) {
                    RawCandidate candidate = new(
                        reader.GetString(0),
                        reader.GetString(1),
                        reader.GetString(2),
                        reader.GetString(3),
                        reader.GetString(4),
                        reader.GetString(5),
                        reader.GetDouble(6));
                    if (runtimeCandidateKeys.Add(CandidateKey(candidate))) {
                        runtimeCandidatesToPrepend.Add(candidate);
                    }
                }
            }
            candidates.RemoveAll(candidate => runtimeCandidateKeys.Contains(CandidateKey(candidate)));
            candidates.InsertRange(0, runtimeCandidatesToPrepend);
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
        IReadOnlyList<HashSet<string>> negativeRoleGroups,
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
        bool requestsGuidance = terms.Overlaps([
            "agent", "agents", "guide", "guidance", "instruction", "instructions",
        ]) || (terms.Overlaps(["policy", "rule", "rules"]) &&
            terms.Overlaps(["repository", "project", "module", "convention", "access", "readonly"]));
        HashSet<string> directTerms = new(directQueryTerms, StringComparer.Ordinal);
        bool explicitlyRequestsTest = terms.Contains("test");
        bool explicitlyRequestsMcp = McpIntent.IsMatch(query);
        bool stronglyRequestsTest = explicitlyRequestsTest &&
            (string.Equals(changeType, "Tests", StringComparison.OrdinalIgnoreCase) ||
                (string.Equals(changeType, "Frontend", StringComparison.OrdinalIgnoreCase) &&
                    directTerms.Contains("tests")));
        Dictionary<string, int> testSubjectWeights = stronglyRequestsTest
            ? GetTestIdentityWeights(directQueryTerms, candidates, policy.DirectFileNameAffinity)
            : [];
        ConversationalAffinity? conversation = policy.ConversationalAffinity;
        bool conversational = conversation is not null && query.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length >= conversation.MinimumWords &&
            (query.Contains('?', StringComparison.Ordinal) || Regex.IsMatch(query, @"^(?:where|find|locate|which|где|как|какие|найди|нужно|хочу)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100)));
        bool frontendIntent = conversational && Regex.IsMatch(query, @"frontend|browser|on the client|client.*(?:api|recovery)|клиент|браузер", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
        bool backendIntent = conversational && Regex.IsMatch(query, @"backend|сервер", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
        bool wikiIntent = conversational && terms.Overlaps(["wiki", "вики"]);
        Dictionary<string, int> subjectWeights = [];
        if (conversational) {
            string[] candidateIdentities = [.. candidates.Select(candidate => new string([.. ExpandSearchText(Path.GetFileName(candidate.Path)).Where(char.IsLetterOrDigit)]))];
            foreach (string term in terms.Where(term => (term.Length >= 3 || string.Equals(term, "ai", StringComparison.Ordinal)) && term.All(character => character is >= 'a' and <= 'z') &&
                !conversation!.ExcludedTerms.Contains(term, StringComparer.Ordinal))) {
                int count = candidateIdentities.Count(identity => identity.Contains(term, StringComparison.OrdinalIgnoreCase));
                if (count > 0) { subjectWeights[term] = Math.Max(0, conversation!.ScorePerSubject - (conversation.FrequencyPenalty * (int)Math.Floor(Math.Log2(count + 1)))); }
            }
        }
        List<RankedCandidate> ranked = [];
        for (int index = 0; index < candidates.Count; index++) {
            RawCandidate candidate = candidates[index];
            string normalizedPath = NormalizePath(candidate.Path).ToLowerInvariant();
            bool domainIntent = policy.GenericAffinities.DomainIntentTerms.Any(term => terms.Contains(term.ToLowerInvariant()));
            string[] selectorPaths = [.. GetRankingPathIdentities(normalizedPath).Where(selectorPath =>
                string.Equals(selectorPath, normalizedPath, StringComparison.Ordinal) || domainIntent ||
                !selectorPath.StartsWith("fooddiary.domain/", StringComparison.Ordinal))];
            bool isTest = TestPath.IsMatch(normalizedPath);
            string fileName = Path.GetFileName(NormalizePath(candidate.Path));
            bool isExplicitTestCandidate = isTest ||
                (fileName.StartsWith("Test", StringComparison.OrdinalIgnoreCase) &&
                    Path.GetExtension(fileName) is ".cs" or ".ps1" or ".ts" or ".js" or ".mjs" or ".cjs");
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
                ExpandSearchText(Path.GetFileName(NormalizePath(candidate.Path))).ToLowerInvariant();
            if (conversational) {
                string subjectIdentity = new([.. searchableFileIdentity.Where(char.IsLetterOrDigit)]);
                int subjectScore = Math.Min(conversation!.MaximumSubjectScore, subjectWeights.Where(pair => subjectIdentity.Contains(pair.Key, StringComparison.Ordinal)).Sum(pair => pair.Value));
                score += subjectScore;
                if (subjectScore > 0) { reasons.Add("conversational subject identity affinity"); }
                bool frontendSource = normalizedPath.StartsWith("fooddiary.web.client/", StringComparison.Ordinal);
                if (!wikiIntent && frontendIntent && frontendSource) { score += conversation.LayerScore; reasons.Add("requested frontend source"); }
                if (!wikiIntent && frontendIntent && !backendIntent && frontendSource && terms.Overlaps(["request", "requests", "api", "http"]) &&
                    (normalizedPath.EndsWith(".service.ts", StringComparison.Ordinal) || normalizedPath.EndsWith(".interceptor.ts", StringComparison.Ordinal))) {
                    score += conversation.LayerScore; reasons.Add("requested frontend transport implementation");
                }
                if (!wikiIntent && backendIntent && !frontendIntent && frontendSource) { score -= conversation.LayerScore; reasons.Add("backend intent excludes frontend source"); }
                if (!isTest && !isExplicitTestCandidate && !wikiIntent && !frontendIntent && (backendIntent || terms.Overlaps(["query", "command"])) && searchableFileIdentity.Contains("handler", StringComparison.Ordinal) &&
                    !terms.Overlaps(["validator", "repository", "controller", "domain", "entity"])) { score += conversation.RoleScore; reasons.Add("application flow handler"); }
                if (wikiIntent && normalizedPath.StartsWith(".llm-wiki/tools/", StringComparison.Ordinal)) {
                    score += isExplicitTestCandidate ? -conversation.LayerScore : conversation.RoleScore;
                    reasons.Add("wiki capability tool role");
                }
            }
            string topLevelModuleIdentity = GetRankingModuleIdentity(normalizedPath);
            string[] normalizedDirectTerms = [.. directQueryTerms.Select(term =>
                new string([.. term.Where(char.IsLetterOrDigit)]))];
            int moduleIdentityTermCount = string.Equals(changeType, "Frontend", StringComparison.OrdinalIgnoreCase) &&
                !normalizedPath.StartsWith("fooddiary.web.client/", StringComparison.Ordinal)
                ? policy.ModuleIdentityLeadingTermCount : policy.ModuleIdentityAffinityLeadingTermCount;
            if (topLevelModuleIdentity.Length >= policy.ModuleIdentityMinimumLength &&
                normalizedDirectTerms.Take(moduleIdentityTermCount).Contains(
                    topLevelModuleIdentity,
                    StringComparer.Ordinal)) {
                score += policy.ModuleIdentityScore;
                reasons.Add($"exact module identity {topLevelModuleIdentity}");
            }
            bool hasAdminIntent = rankingTerms.Any(term => policy.AdminIntentTerms.Any(intent =>
                term.StartsWith(intent, StringComparison.Ordinal)));
            string[] adminIdentityEvidence = [.. queryTerms.Where(term =>
                term.Length >= policy.PathTermAffinity.MinimumTermLength &&
                searchableIdentity.Contains(term, StringComparison.Ordinal))];
            if (normalizedPath.Contains("admin", StringComparison.Ordinal) &&
                !hasAdminIntent &&
                adminIdentityEvidence.Length < policy.UnrequestedAdminPenaltyExemptionMatches) {
                score -= policy.UnrequestedAdminPenalty;
                reasons.Add("admin candidate penalty without admin intent");
            }
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
            string[] directFileNameMatches = [.. directTerms.Where(term =>
                (term.Length >= policy.DirectFileNameAffinity.MinimumTermLength ||
                    (term.Length >= 2 && term.All(char.IsLetterOrDigit) && term.Any(char.IsLetter) && term.Any(char.IsDigit))) &&
                (searchableFileIdentity.Contains(term, StringComparison.Ordinal) || (isExplicitTestCandidate && stronglyRequestsTest &&
                    GetEnglishMorphologicalVariants(term).Any(variant => searchableFileIdentity.Contains(variant, StringComparison.Ordinal)))))];
            int directFileNameScore = Math.Min(
                directFileNameMatches.Length * policy.DirectFileNameAffinity.ScorePerMatch,
                policy.DirectFileNameAffinity.MaximumScore);
            if (directFileNameScore > 0) {
                score += directFileNameScore;
                reasons.Add($"direct file-name affinity {string.Join(", ", directFileNameMatches)}");
            }
            GenericAffinities genericAffinity = policy.GenericAffinities;
            string[] explicitRoleMatches = [.. genericAffinity.RoleTerms.Where(term => {
                string normalizedTerm = term.ToLowerInvariant();
                if (normalizedTerm is "consumer" or "consumers" && !terms.Contains("powershell")) {
                    return false;
                }
                return terms.Contains(normalizedTerm) &&
                    searchableFileIdentity.Contains(normalizedTerm, StringComparison.Ordinal);
            })];
            int explicitRoleScore = Math.Min(
                explicitRoleMatches.Sum(term => genericAffinity.RoleScoreOverrides?.TryGetValue(
                    term,
                    out int overrideScore) == true
                    ? overrideScore
                    : genericAffinity.RoleScorePerMatch),
                genericAffinity.MaximumRoleScore);
            if (explicitRoleScore > 0) {
                score += explicitRoleScore;
                reasons.Add($"generic file-role affinity {string.Join(", ", explicitRoleMatches)}");
            }
            void ApplyGenericPathAffinity(
                string id,
                IReadOnlyList<string> intentTerms,
                IReadOnlyList<string> pathValues,
                int value,
                bool suffix = false,
                int minimumMatches = 1) {
                int intentMatchCount = intentTerms.Count(term => terms.Contains(term.ToLowerInvariant()));
                bool intentMatched = intentMatchCount >= minimumMatches;
                bool pathMatched = pathValues.Any(pathValue => suffix
                    ? normalizedPath.EndsWith(pathValue.ToLowerInvariant(), StringComparison.Ordinal)
                    : selectorPaths.Any(selectorPath => selectorPath.Contains(
                        NormalizePath(pathValue).ToLowerInvariant(),
                        StringComparison.Ordinal)));
                if (!intentMatched || !pathMatched || value == 0) {
                    return;
                }
                score += value;
                reasons.Add($"generic {id} affinity");
            }
            void ApplyChangeTypePathAffinity(
                string id,
                string expectedType,
                IReadOnlyList<string> pathValues,
                int value) {
                if (!string.Equals(changeType, expectedType, StringComparison.OrdinalIgnoreCase) ||
                    !pathValues.Any(pathValue => selectorPaths.Any(selectorPath => selectorPath.Contains(
                        NormalizePath(pathValue).ToLowerInvariant(),
                        StringComparison.Ordinal)))) {
                    return;
                }
                score += value;
                reasons.Add($"generic {id} affinity");
            }
            if (!string.Equals(changeType, "Database", StringComparison.OrdinalIgnoreCase)) {
                ApplyGenericPathAffinity(
                    "domain-layer",
                    genericAffinity.DomainIntentTerms,
                    genericAffinity.DomainPathPrefixes,
                    genericAffinity.DomainScore);
            }
            ApplyGenericPathAffinity(
                "api-layer",
                genericAffinity.ApiIntentTerms,
                genericAffinity.ApiPathFragments,
                genericAffinity.ApiScore);
            ApplyGenericPathAffinity(
                "database-layer",
                genericAffinity.DatabaseIntentTerms,
                genericAffinity.DatabasePathFragments,
                genericAffinity.DatabaseScore);
            ApplyChangeTypePathAffinity(
                "api-change-type",
                "Api",
                genericAffinity.ApiPathFragments,
                genericAffinity.ApiScore);
            ApplyChangeTypePathAffinity(
                "database-change-type",
                "Database",
                genericAffinity.DatabasePathFragments,
                genericAffinity.DatabaseScore);
            ApplyGenericPathAffinity(
                "admin-scope",
                genericAffinity.AdminIntentTerms,
                genericAffinity.AdminPathFragments,
                genericAffinity.AdminScore);
            ApplyGenericPathAffinity(
                "integration-layer",
                genericAffinity.IntegrationIntentTerms,
                genericAffinity.IntegrationPathPrefixes,
                genericAffinity.IntegrationScore);
            bool excludesInfrastructureAffinity = genericAffinity.InfrastructureExcludedIntentTerms
                .Any(term => terms.Contains(term.ToLowerInvariant())) ||
                // Moving a provider under its owner must not add a second layer bonus.
                (normalizedPath.StartsWith("modules/", StringComparison.Ordinal) &&
                    selectorPaths.Any(path => path.StartsWith("fooddiary.integrations/", StringComparison.Ordinal)));
            if (!excludesInfrastructureAffinity) {
                ApplyGenericPathAffinity(
                    "infrastructure-layer",
                    genericAffinity.InfrastructureIntentTerms,
                    genericAffinity.InfrastructurePathFragments,
                    genericAffinity.InfrastructureScore,
                    minimumMatches: genericAffinity.InfrastructureMinimumMatches);
            }
            ApplyGenericPathAffinity(
                "node-runtime",
                genericAffinity.NodeIntentTerms,
                genericAffinity.NodePathSuffixes,
                genericAffinity.NodeScore,
                suffix: true);
            if (!explicitlyRequestsMcp) {
                ApplyGenericPathAffinity(
                    "powershell-runtime",
                    genericAffinity.PowershellIntentTerms,
                    genericAffinity.PowershellPathSuffixes,
                    genericAffinity.PowershellScore,
                    suffix: true);
            }
            if (!string.Equals(changeType, "Frontend", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(changeType, "Tests", StringComparison.OrdinalIgnoreCase) &&
                !terms.Contains("how") &&
                !explicitlyRequestsMcp) {
                ApplyGenericPathAffinity(
                    "wiki-tooling",
                    genericAffinity.WikiToolIntentTerms,
                    genericAffinity.WikiToolPathPrefixes,
                    genericAffinity.WikiToolScore);
            }
            if (isExplicitTestCandidate && stronglyRequestsTest) {
                int subjectScore = Math.Min(
                    directFileNameMatches.Sum(term => testSubjectWeights.GetValueOrDefault(term)),
                    policy.DirectFileNameAffinity.MaximumScore);
                score += subjectScore;
                if (subjectScore > 0) {
                    reasons.Add(FormattableString.Invariant($"direct test-subject specificity {subjectScore}"));
                }
                int explicitTestScore = Math.Min(
                    identityMatches.Length * policy.ExplicitTestAffinity.ScorePerMatch,
                    policy.ExplicitTestAffinity.MaximumScore);
                score += explicitTestScore;
                if (explicitTestScore > 0) {
                    reasons.Add($"explicit test behavior affinity {identityMatches.Length} terms");
                }
            }
            if (!isExplicitTestCandidate && stronglyRequestsTest) {
                score -= policy.ExplicitTestAffinity.NonTestPenalty;
                reasons.Add("production candidate penalty for explicit test intent");
            }
            foreach (IdentityBoost boost in policy.IdentityBoosts) {
                if (explicitlyRequestsMcp && string.Equals(
                    boost.Id,
                    "explicit-powershell-file-intent",
                    StringComparison.Ordinal)) {
                    continue;
                }
                bool matchesChangeType = boost.ChangeTypes is null ||
                    boost.ChangeTypes.Length == 0 ||
                    boost.ChangeTypes.Any(candidateChangeType =>
                        string.Equals(candidateChangeType, changeType, StringComparison.OrdinalIgnoreCase));
                if (!matchesChangeType) {
                    continue;
                }
                HashSet<string> eligibleQueryTerms = boost.DirectOnly ? directTerms : terms;
                if (string.Equals(changeType, "Tests", StringComparison.OrdinalIgnoreCase) &&
                    isTest &&
                    !boost.Id.Contains("test", StringComparison.OrdinalIgnoreCase)) {
                    continue;
                }
                if (boost.ExcludedQueryTerms?.Any(term =>
                    eligibleQueryTerms.Contains(term.ToLowerInvariant())) == true) {
                    continue;
                }
                string eligibleIdentity = boost.IdentityScope?.ToLowerInvariant() switch {
                    "file" => searchableFileIdentity,
                    "identity" => searchableIdentity,
                    _ => searchablePath,
                };
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
                        selectorPaths.Any(selectorPath => selectorPath.StartsWith(NormalizePath(prefix).ToLowerInvariant(), StringComparison.Ordinal)))) ||
                    (boost.ExcludedPathPrefixes is { Length: > 0 } && boost.ExcludedPathPrefixes.Any(prefix =>
                        selectorPaths.Any(selectorPath => selectorPath.StartsWith(NormalizePath(prefix).ToLowerInvariant(), StringComparison.Ordinal)))) ||
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
                if (boost.ExcludedQueryTerms?.Any(term =>
                    eligibleQueryTerms.Contains(term.ToLowerInvariant())) == true) {
                    continue;
                }
                int matchedTerms = boost.QueryTerms.Count(term =>
                    eligibleQueryTerms.Contains(term.ToLowerInvariant()));
                bool matchesPath = boost.PathPrefixes.Any(prefix =>
                    selectorPaths.Any(selectorPath => selectorPath.StartsWith(NormalizePath(prefix).ToLowerInvariant(), StringComparison.Ordinal)));
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
            foreach (HashSet<string> negativeRoleGroup in negativeRoleGroups) {
                string[] matchedNegativeRoles = [.. negativeRoleGroup.Where(term =>
                    searchableFileIdentity.Contains(term, StringComparison.Ordinal))];
                int requiredMatches = Math.Min(2, negativeRoleGroup.Count);
                if (matchedNegativeRoles.Length < requiredMatches) {
                    continue;
                }
                int penalty = Math.Min(
                    matchedNegativeRoles.Length * policy.NegatedRolePenalty.ScorePerMatch,
                    policy.NegatedRolePenalty.MaximumScorePerPhrase);
                score -= penalty;
                reasons.Add($"negated role penalty {string.Join(", ", matchedNegativeRoles)}");
            }
            if (isExplicitTestCandidate &&
                !string.Equals(changeType, "Tests", StringComparison.OrdinalIgnoreCase) &&
                !stronglyRequestsTest) {
                score -= policy.NonTestPenalty;
                reasons.Add("test candidate ranked after production");
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
            bool requestsDocumentation = policy.DocumentationImplementationPenalty.RequestTerms
                .Any(term => terms.Contains(term.ToLowerInvariant()));
            bool isDocumentationCandidate =
                string.Equals(candidate.RecordType, "agent-guide", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(candidate.RecordType, "documentation", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(candidate.RecordType, "wiki-page", StringComparison.OrdinalIgnoreCase) ||
                normalizedPath.EndsWith("/agents.md", StringComparison.Ordinal) ||
                string.Equals(normalizedPath, "agents.md", StringComparison.Ordinal) ||
                (normalizedPath.StartsWith("docs/", StringComparison.Ordinal) &&
                    normalizedPath.EndsWith(".md", StringComparison.Ordinal));
            bool implicitImplementationIntent = string.Equals(changeType, "Any", StringComparison.OrdinalIgnoreCase) &&
                new[] {
                    genericAffinity.DomainIntentTerms, genericAffinity.ApiIntentTerms,
                    genericAffinity.DatabaseIntentTerms, genericAffinity.IntegrationIntentTerms,
                }
                .Any(intentTerms => intentTerms.Any(term => terms.Contains(term.ToLowerInvariant())));
            implicitImplementationIntent |= string.Equals(changeType, "Any", StringComparison.OrdinalIgnoreCase) &&
                !genericAffinity.InfrastructureExcludedIntentTerms.Any(term => terms.Contains(term.ToLowerInvariant())) &&
                genericAffinity.InfrastructureIntentTerms.Count(term => terms.Contains(term.ToLowerInvariant())) >=
                    genericAffinity.InfrastructureMinimumMatches;
            if (isDocumentationCandidate && !requestsDocumentation &&
                (implicitImplementationIntent || policy.DocumentationImplementationPenalty.ChangeTypes.Any(candidateChangeType =>
                    string.Equals(candidateChangeType, changeType, StringComparison.OrdinalIgnoreCase)))) {
                score -= policy.DocumentationImplementationPenalty.Score;
                reasons.Add("documentation candidate penalty for implementation intent");
            }
            bool moduleEntryPoint = IsModuleEntryPointQuery(normalizedPath, changeType, directQueryTerms, terms, policy);
            bool requestsAbstraction = moduleEntryPoint || terms.Overlaps(["interface", "contract", "abstraction"]);
            if (moduleEntryPoint) {
                reasons.Add("module entry-point abstraction penalty waived");
            }
            if (!requestsAbstraction && selectorPaths.Any(selectorPath => selectorPath.StartsWith(
                "fooddiary.application.abstractions/",
                StringComparison.Ordinal))) {
                score -= policy.ApplicationAbstractionPenalty;
            }
            if (!requestsAbstraction && InterfacePath.IsMatch(candidate.Path)) {
                score -= policy.InterfacePathPenalty;
            }
            if (!string.Equals(changeType, "Tests", StringComparison.OrdinalIgnoreCase) &&
                CompanionPath.IsMatch(candidate.Path) &&
                directFileNameMatches.Length < policy.DirectFileNameAffinity.CompanionPenaltyExemptionMatches) {
                score -= policy.CompanionFilePenalty;
                reasons.Add("companion file ranked after primary declaration");
            }
            if (isCode) {
                score += 20;
            }
            if (string.Equals(candidate.RecordType, "agent-guide", StringComparison.Ordinal)) {
                score += requestsGuidance ? policy.AgentGuideBoost : -policy.AgentGuideBoost;
                reasons.Add(requestsGuidance
                    ? "agent guide affinity"
                    : "agent guide penalty for code intent");
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
        }
        bool unmatchedIdentifier = ExplicitIdentifier.Matches(query).Select(match => match.Value)
            .Any(value => (value.Any(char.IsDigit) || CamelBoundary.IsMatch(value)) && !candidates.Any(candidate => candidate.Path.Contains(value, StringComparison.OrdinalIgnoreCase) ||
                candidate.Title.Contains(value, StringComparison.OrdinalIgnoreCase)));
        var fileNameCounts = result
            .GroupBy(candidate => Path.GetFileName(candidate.Path), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);
        List<WikiContextSearchCandidate> decorated = [.. result.Select((candidate, index) => {
            int? scoreMargin = index + 1 < result.Count
                ? candidate.Score - result[index + 1].Score
                : null;
            string sameNameKey = Path.GetFileName(candidate.Path).ToLowerInvariant();
            int sameNameCandidateCount = fileNameCounts[sameNameKey];
            bool recordTypeMismatch = policy.ConfidenceCalibration.ImplementationChangeTypes.Any(candidateChangeType =>
                    string.Equals(candidateChangeType, changeType, StringComparison.OrdinalIgnoreCase)) &&
                policy.ConfidenceCalibration.DocumentationRecordTypes.Any(recordType =>
                    string.Equals(recordType, candidate.RecordType, StringComparison.OrdinalIgnoreCase));
            bool ambiguous = unmatchedIdentifier || recordTypeMismatch || (scoreMargin is not null &&
                scoreMargin <= policy.ConfidenceCalibration.AmbiguityMaximumMargin);
            string? ambiguityReason = null;
            if (unmatchedIdentifier) {
                ambiguityReason = "unmatched-query-identifier";
            } else if (recordTypeMismatch) {
                ambiguityReason = "record-type-change-type-mismatch";
            } else if (ambiguous) {
                ambiguityReason = "top-score-margin";
            }
            string confidence;
            if (ambiguous) {
                confidence = "low";
            } else if (scoreMargin is null) {
                confidence = "unknown";
            } else if (scoreMargin >= policy.ConfidenceCalibration.HighMinimumMargin) {
                confidence = "high";
            } else {
                confidence = scoreMargin >= policy.ConfidenceCalibration.MediumMinimumMargin ? "medium" : "low";
            }
            return candidate with {
                ScoreMargin = scoreMargin,
                Confidence = confidence,
                Ambiguous = ambiguous,
                AmbiguityReason = ambiguityReason,
                SameNameCandidateCount = sameNameCandidateCount,
            };
        })];
        List<WikiContextSearchCandidate> selected = PreserveScopeCoverage(
            decorated,
            normalizedScopes,
            limit);
        return [.. selected.Select((candidate, index) => candidate with { Rank = index + 1 })];
    }

    private static List<WikiContextSearchCandidate> PreserveScopeCoverage(
        IReadOnlyList<WikiContextSearchCandidate> ranked,
        IReadOnlyList<string> normalizedScopes,
        int limit) {
        List<WikiContextSearchCandidate> selected = [.. ranked.Take(limit)];
        if (selected.Count == 0 || normalizedScopes.Count == 0 || limit < normalizedScopes.Count) {
            return selected;
        }

        foreach (string scope in normalizedScopes) {
            if (selected.Any(candidate => IsWithinScope(candidate.Path, scope))) {
                continue;
            }

            WikiContextSearchCandidate? scopedCandidate = ranked.FirstOrDefault(candidate =>
                IsWithinScope(candidate.Path, scope));
            if (scopedCandidate is null || selected.Contains(scopedCandidate)) {
                continue;
            }

            int replacementIndex = -1;
            for (int index = selected.Count - 1; index >= 0; index--) {
                WikiContextSearchCandidate current = selected[index];
                bool isOnlyRepresentative = normalizedScopes.Any(candidateScope =>
                    IsWithinScope(current.Path, candidateScope) &&
                    selected.Count(candidate => IsWithinScope(candidate.Path, candidateScope)) == 1);
                if (!isOnlyRepresentative) {
                    replacementIndex = index;
                    break;
                }
            }
            if (replacementIndex >= 0) {
                selected[replacementIndex] = scopedCandidate;
                selected.Sort((left, right) => {
                    int scoreComparison = right.Score.CompareTo(left.Score);
                    return scoreComparison != 0
                        ? scoreComparison
                        : StringComparer.Ordinal.Compare(left.Path, right.Path);
                });
            }
        }
        return selected;
    }

    private static bool IsWithinScope(string path, string normalizedScope) {
        string normalizedPath = NormalizePath(path).ToLowerInvariant();
        return normalizedPath.Equals(normalizedScope, StringComparison.Ordinal) ||
            normalizedPath.StartsWith($"{normalizedScope}/", StringComparison.Ordinal) ||
            normalizedScope.StartsWith($"{normalizedPath}/", StringComparison.Ordinal);
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
        if (query.Contains('?', StringComparison.Ordinal) || Regex.IsMatch(query, @"^(?:where|find|locate|which|где|как|какие|найди|нужно|хочу)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100))) {
            foreach (string term in directTerms) { AddExpansions(GetEnglishMorphologicalVariants(term), terms, seen); }
        }
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
        foreach (Match match in HyphenatedIdentifier.Matches(query.ToLowerInvariant())) {
            string compact = match.Value.Replace("-", string.Empty, StringComparison.Ordinal);
            if (!stopTerms.Contains(compact) && seen.Add(compact)) {
                terms.Add(compact);
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
            foreach ((string termGroup, string[] expansions) in policy.QueryTermExpansions) {
                if (termGroup.Split('|', StringSplitOptions.RemoveEmptyEntries).Contains(term, StringComparer.Ordinal)) {
                    AddExpansions(expansions.Contains("@inflect", StringComparer.Ordinal) ? [.. GetEnglishMorphologicalVariants(term).Take(1)] : expansions, terms, seen);
                }
            }
            foreach ((string prefix, string[] prefixExpansions) in policy.QueryPrefixExpansions) {
                if (prefix.Split('|', StringSplitOptions.RemoveEmptyEntries).Any(candidate =>
                    term.StartsWith(candidate, StringComparison.Ordinal))) {
                    AddExpansions(prefixExpansions, terms, seen);
                }
            }
        }
        foreach (QueryContextExpansion expansion in policy.QueryContextExpansions ?? []) {
            if (expansion.RequiredTerms.All(seen.Contains)) { AddExpansions(expansion.Terms, terms, seen); }
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
        if (term.Length > 4 && new[] { "xes", "ches", "shes", "sses", "zes" }
            .Any(suffix => term.EndsWith(suffix, StringComparison.Ordinal))) {
            variants.Add(term[..^2]);
        }
        if (term.Length > 5 && term.EndsWith("ing", StringComparison.Ordinal)) {
            string stem = term[..^3];
            variants.Add(stem);
            variants.Add($"{stem}e");
            if (stem.Length > 2 && stem[^1] == stem[^2]) {
                variants.Add(stem[..^1]);
            }
        }
        if (term.Length > 4 && term.EndsWith("ied", StringComparison.Ordinal)) { variants.Add($"{term[..^3]}y"); }
        if (term.Length > 4 && term.EndsWith("ed", StringComparison.Ordinal)) {
            variants.Add(term[..^2]);
            variants.Add(term[..^1]);
        }
        return [.. variants];
    }

    private static IReadOnlyList<HashSet<string>> GetNegatedRoleTermGroups(
        string query,
        RankingPolicy policy) {
        if (policy.NegatedRolePenalty.Markers.Length == 0 ||
            policy.NegatedRolePenalty.RoleTerms.Length == 0) {
            return [];
        }
        string markerPattern = string.Join(
            '|',
            policy.NegatedRolePenalty.Markers.Select(Regex.Escape));
        Regex expression = new(
            $@"(?:^|\s)(?:{markerPattern})\s+(?<phrase>.+?)(?=(?:\s+(?:и\s+)?(?:{markerPattern})\s+)|[,;:—–]|\s+(?:а|but)\s+|$)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
            TimeSpan.FromMilliseconds(100));
        HashSet<string> roleTerms = new(
            policy.NegatedRolePenalty.RoleTerms.Select(term => term.ToLowerInvariant()),
            StringComparer.Ordinal);
        List<HashSet<string>> groups = [];
        foreach (Match match in expression.Matches(ExpandSearchText(query).ToLowerInvariant())) {
            HashSet<string> group = new(StringComparer.Ordinal);
            foreach (string term in GetDirectQueryTerms(match.Groups["phrase"].Value, policy)) {
                if (roleTerms.Contains(term)) {
                    group.Add(term);
                    continue;
                }
                List<string> expansions = [];
                AddConfiguredExpansions(
                    [term],
                    policy,
                    expansions,
                    new HashSet<string>(StringComparer.Ordinal));
                group.UnionWith(expansions.Where(roleTerms.Contains));
            }
            if (group.Count > 0) {
                groups.Add(group);
            }
        }
        return groups;
    }

    private static string[] GetNegatedRoleAlternatives(
        IReadOnlyList<HashSet<string>> negativeRoleGroups,
        RankingPolicy policy) =>
        [.. negativeRoleGroups
            .SelectMany(group => group)
            .Where(policy.NegatedRoleAlternatives.ContainsKey)
            .SelectMany(term => policy.NegatedRoleAlternatives[term])
            .Distinct(StringComparer.Ordinal)];

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
            policy.CandidatePoolLimit < 1 ||
            policy.IdentityCandidatePoolLimit < 1 ||
            policy.StopTerms is null ||
            policy.PathTermAffinity is null ||
            policy.DirectFileNameAffinity is null ||
            policy.DirectFileNameAffinity.CompanionPenaltyExemptionMatches < 1 ||
            policy.MatchedPolicyFileNameAffinity is null ||
            policy.ExplicitTestAffinity is null ||
            policy.NegatedRolePenalty is null ||
            policy.QueryTermExpansions is null ||
            policy.QueryPrefixExpansions is null ||
            policy.ModuleIdentityMinimumLength < 1 ||
            policy.ModuleIdentityLeadingTermCount < 1 ||
            policy.ModuleIdentityAffinityLeadingTermCount < 1 ||
            policy.AdminIntentTerms is null ||
            policy.UnrequestedAdminPenaltyExemptionMatches < 1 ||
            policy.DocumentationImplementationPenalty is null ||
            policy.DocumentationImplementationPenalty.ChangeTypes is null ||
            policy.DocumentationImplementationPenalty.RequestTerms is null ||
            policy.NegatedRoleAlternatives is null ||
            policy.PathBoosts is null ||
            policy.IdentityBoosts is null ||
            policy.GenericAffinities is null ||
            policy.GenericAffinities.InfrastructureIntentTerms is null ||
            policy.GenericAffinities.InfrastructureMinimumMatches < 1 ||
            policy.GenericAffinities.InfrastructureExcludedIntentTerms is null ||
            policy.GenericAffinities.InfrastructurePathFragments is null ||
            policy.IdentityBoosts?.Any(boost =>
                !string.IsNullOrWhiteSpace(boost.IdentityScope) &&
                !string.Equals(boost.IdentityScope, "path", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(boost.IdentityScope, "file", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(boost.IdentityScope, "identity", StringComparison.OrdinalIgnoreCase)) == true ||
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

    private static string[] GetRankingPathIdentities(string path) {
        string[] rootParts = path.Split('/', 3);
        if (rootParts.Length == 3 && rootParts[0] is "shared" or "tooling" && string.Equals(rootParts[1], "tests", StringComparison.Ordinal)) {
            string[] testPath = rootParts[2].Split('/', 2);
            if (testPath.Length == 2 && testPath[0].EndsWith(".tests", StringComparison.Ordinal) && testPath[1].Length > 0) {
                return [path, $"tests/{rootParts[2]}"];
            }
        }
        const string integrationsPrefix = "shared/fooddiary.integrations.http/";
        if (rootParts.Length == 3 && string.Equals(rootParts[0], "shared", StringComparison.Ordinal) && !TestPath.IsMatch(path)) {
            string[] projectParts = rootParts[1].Split('.');
            if (projectParts.Length == 3 && string.Equals(projectParts[0], "fooddiary", StringComparison.Ordinal) && projectParts[1].Length > 0 &&
                string.Equals(projectParts[2], "persistencemodel", StringComparison.Ordinal) && rootParts[2].Length > 0) {
                string modelTail = rootParts[2];
                return [path, modelTail.StartsWith("configurations/", StringComparison.Ordinal)
                    ? $"fooddiary.infrastructure/persistence/configurations/{projectParts[1]}/{modelTail["configurations/".Length..]}"
                    : $"fooddiary.infrastructure/persistence/{projectParts[1]}/{modelTail}"];
            }
        }
        if (path.StartsWith(integrationsPrefix, StringComparison.Ordinal) && !TestPath.IsMatch(path)) {
            return [path, $"fooddiary.integrations/{path[integrationsPrefix.Length..]}"];
        }
        const string primitivesPrefix = "shared/fooddiary.domain.primitives/";
        if (path.StartsWith(primitivesPrefix, StringComparison.Ordinal) && !TestPath.IsMatch(path)) {
            return [path, $"fooddiary.domain/{path[primitivesPrefix.Length..]}"];
        }
        string[] parts = path.Split('/', 4);
        if (parts.Length == 4 && string.Equals(parts[0], "modules", StringComparison.Ordinal) &&
            string.Equals(parts[2], "tests", StringComparison.Ordinal)) {
            string[] testParts = parts[3].Split('/', 2);
            string prefix = $"fooddiary.modules.{parts[1]}.";
            if (testParts.Length == 2 && testParts[1].Length > 0 && testParts[0].StartsWith(prefix, StringComparison.Ordinal)) {
                string testProject = testParts[0][prefix.Length..];
                if (testProject is "application.tests" or "domain.tests" or "infrastructure.tests" or
                    "infrastructure.integration.tests" or "infrastructure.integrationtests") {
                    return [path, $"tests/fooddiary.{testProject}/{testParts[1]}"];
                }
            }
        }
        if (parts.Length != 4 || !string.Equals(parts[0], "modules", StringComparison.Ordinal) || parts[3].Length == 0 || TestPath.IsMatch(path)) {
            return [path];
        }
        string tail = parts[3];
        string? alias = parts[2] switch {
            "presentation" => $"fooddiary.presentation.api/{tail}",
            // Consumer contracts keep abstraction selectors; they gain no implementation layer.
            "contracts" => $"fooddiary.application.abstractions/{tail}",
            "application" => tail.StartsWith("abstractions/", StringComparison.Ordinal)
                ? $"fooddiary.application.abstractions/{tail["abstractions/".Length..]}"
                : $"fooddiary.application.{parts[1]}/{tail}",
            "domain" => $"fooddiary.domain/{tail}",
            "infrastructure" when tail.StartsWith("providers/", StringComparison.Ordinal) =>
                $"fooddiary.integrations/{tail["providers/".Length..]}",
            "infrastructure" => tail.StartsWith("model/", StringComparison.Ordinal)
                ? $"fooddiary.infrastructure/persistence/{tail["model/".Length..]}"
                : $"fooddiary.infrastructure/{tail}",
            _ => null,
        };
        return alias is null ? [path] : [path, alias];
    }

    private static string GetRankingModuleIdentity(string path) {
        string[] parts = path.Split('/');
        string root = parts.Length >= 4 && string.Equals(parts[0], "modules", StringComparison.Ordinal) ? parts[1] : parts[0];
        if (root.StartsWith("fooddiary.application.", StringComparison.Ordinal)) {
            root = root["fooddiary.application.".Length..];
        }
        if (root.StartsWith("fooddiary.", StringComparison.Ordinal)) {
            root = root["fooddiary.".Length..];
        }
        return new string([.. root.Where(char.IsLetterOrDigit)]);
    }

    // Keep this penalty waiver and the integer frequency buckets in parity with
    // code-graph-path-layout.mjs. Neither changes candidate recall or module identity.
    private static bool IsModuleEntryPointQuery(
        string path,
        string changeType,
        IReadOnlyList<string> directTerms,
        HashSet<string> terms,
        RankingPolicy policy) {
        string[] parts = path.Split('/', 4);
        if (changeType.ToLowerInvariant() is not ("backend" or "any") ||
            parts.Length != 4 || !string.Equals(parts[0], "modules", StringComparison.Ordinal) ||
            !string.Equals(parts[2], "application", StringComparison.Ordinal) || TestPath.IsMatch(path)) {
            return false;
        }
        string module = GetRankingModuleIdentity(path);
        if (module.Length < policy.ModuleIdentityMinimumLength ||
            !directTerms.Take(policy.ModuleIdentityLeadingTermCount)
                .Select(term => new string([.. term.Where(char.IsLetterOrDigit)]))
                .Contains(module, StringComparer.Ordinal)) {
            return false;
        }
        GenericAffinities affinity = policy.GenericAffinities;
        return !affinity.DomainIntentTerms.Concat(affinity.ApiIntentTerms)
            .Concat(affinity.DatabaseIntentTerms).Concat(affinity.IntegrationIntentTerms)
            .Concat(affinity.InfrastructureIntentTerms)
            .Concat(["implementation", "options", "configuration", "test"])
            .Any(term => terms.Contains(term.ToLowerInvariant()));
    }

    private static Dictionary<string, int> GetTestIdentityWeights(
        IReadOnlyList<string> directTerms,
        IReadOnlyList<RawCandidate> candidates,
        DirectFileNameAffinity affinity) {
        string[] identities = [.. candidates.Select(candidate => NormalizePath(candidate.Path).ToLowerInvariant())
            .Distinct(StringComparer.Ordinal)
            .Select(path => ExpandSearchText(Path.GetFileName(path)).ToLowerInvariant())];
        Dictionary<string, int> weights = new(StringComparer.Ordinal);
        foreach (string term in directTerms.Distinct(StringComparer.Ordinal)) {
            if (term is "test" or "tests" or "spec" or "specs" or "feature" or "features") {
                continue;
            }
            if (!(term.Length >= affinity.MinimumTermLength ||
                (term.Length >= 2 && term.All(char.IsLetterOrDigit) && term.Any(char.IsLetter) && term.Any(char.IsDigit)))) {
                continue;
            }
            int frequency = identities.Count(identity => identity.Contains(term, StringComparison.Ordinal));
            int buckets = frequency == 0 ? 0 : (int)Math.Floor(Math.Log2((identities.Length + 1d) / (frequency + 1d)));
            weights[term] = Math.Min(buckets * affinity.ScorePerMatch, affinity.MaximumScore);
        }
        return weights;
    }

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
        int CandidatePoolLimit,
        int IdentityCandidatePoolLimit,
        string[] StopTerms,
        PathTermAffinity PathTermAffinity,
        DirectFileNameAffinity DirectFileNameAffinity,
        PathTermAffinity MatchedPolicyFileNameAffinity,
        ScoreCap ExplicitTestAffinity,
        NegatedRolePenalty NegatedRolePenalty,
        int NonTestPenalty,
        int ApplicationAbstractionPenalty,
        int InterfacePathPenalty,
        int AgentGuideBoost,
        int CompanionFilePenalty,
        int CrossLayerPenalty,
        int ModuleIdentityMinimumLength,
        int ModuleIdentityLeadingTermCount,
        int ModuleIdentityAffinityLeadingTermCount,
        int ModuleIdentityScore,
        string[] AdminIntentTerms,
        int UnrequestedAdminPenalty,
        int UnrequestedAdminPenaltyExemptionMatches,
        DocumentationImplementationPenalty DocumentationImplementationPenalty,
        ConfidenceCalibration ConfidenceCalibration,
        Dictionary<string, string[]> QueryTermExpansions,
        Dictionary<string, string[]> QueryPrefixExpansions,
        Dictionary<string, string[]> NegatedRoleAlternatives,
        PathBoost[] PathBoosts,
        IdentityBoost[] IdentityBoosts,
        GenericAffinities GenericAffinities,
        StructuralRoleBoost[]? StructuralRoleBoosts = null,
        ConversationalAffinity? ConversationalAffinity = null,
        QueryContextExpansion[]? QueryContextExpansions = null);

    private sealed record QueryContextExpansion(string[] RequiredTerms, string[] Terms);

    private sealed record ConversationalAffinity(int MinimumWords, int ScorePerSubject, int MaximumSubjectScore, int FrequencyPenalty, int LayerScore, int RoleScore, string[] ExcludedTerms);

    private sealed record ConfidenceCalibration(
        int AmbiguityMaximumMargin,
        int HighMinimumMargin,
        int MediumMinimumMargin,
        string[] ImplementationChangeTypes,
        string[] DocumentationRecordTypes);

    private sealed record GenericAffinities(
        int RoleScorePerMatch,
        int MaximumRoleScore,
        string[] RoleTerms,
        string[] DomainIntentTerms,
        string[] DomainPathPrefixes,
        int DomainScore,
        string[] ApiIntentTerms,
        string[] ApiPathFragments,
        int ApiScore,
        string[] DatabaseIntentTerms,
        string[] DatabasePathFragments,
        int DatabaseScore,
        string[] AdminIntentTerms,
        string[] AdminPathFragments,
        int AdminScore,
        string[] IntegrationIntentTerms,
        string[] IntegrationPathPrefixes,
        int IntegrationScore,
        string[] InfrastructureIntentTerms,
        int InfrastructureMinimumMatches,
        string[] InfrastructureExcludedIntentTerms,
        string[] InfrastructurePathFragments,
        int InfrastructureScore,
        string[] NodeIntentTerms,
        string[] NodePathSuffixes,
        int NodeScore,
        string[] PowershellIntentTerms,
        string[] PowershellPathSuffixes,
        int PowershellScore,
        string[] WikiToolIntentTerms,
        string[] WikiToolPathPrefixes,
        int WikiToolScore,
        Dictionary<string, int>? RoleScoreOverrides = null);

    private sealed record PathTermAffinity(
        int MinimumTermLength,
        int ScorePerMatch,
        int MaximumScore);

    private sealed record DirectFileNameAffinity(
        int MinimumTermLength,
        int ScorePerMatch,
        int MaximumScore,
        int CompanionPenaltyExemptionMatches);

    private sealed record DocumentationImplementationPenalty(
        int Score,
        string[] ChangeTypes,
        string[] RequestTerms);

    private sealed record ScoreCap(
        int ScorePerMatch,
        int MaximumScore,
        int NonTestPenalty = 0);

    private sealed record NegatedRolePenalty(
        string[] Markers,
        string[] RoleTerms,
        int ScorePerMatch,
        int MaximumScorePerPhrase);

    private sealed record PathBoost(
        string Id,
        string[] QueryTerms,
        int MinimumMatches,
        string[] PathPrefixes,
        int Score,
        bool DirectOnly = false,
        string[]? ExcludedQueryTerms = null);

    private sealed record IdentityBoost(
        string Id,
        string[] QueryTerms,
        int MinimumMatches,
        string[] IdentityTerms,
        int MinimumIdentityMatches,
        int Score,
        bool DirectOnly = false,
        string? IdentityScope = null,
        string[]? ChangeTypes = null,
        string[]? ExcludedQueryTerms = null);

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
