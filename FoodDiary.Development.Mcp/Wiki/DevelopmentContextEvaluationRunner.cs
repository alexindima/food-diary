using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using FoodDiary.Development.Mcp.Protocol;

namespace FoodDiary.Development.Mcp.Wiki;

internal static class DevelopmentContextEvaluationRunner {
    private static readonly JsonSerializerOptions InputOptions = new(JsonSerializerDefaults.Web);
    private static readonly JsonSerializerOptions OutputOptions = new(JsonSerializerDefaults.Web) {
        WriteIndented = true,
    };

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public static async Task RunAsync(
        WikiQueryService queries,
        string corpusPath,
        TextWriter output,
        CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(queries);
        ArgumentException.ThrowIfNullOrWhiteSpace(corpusPath);
        ArgumentNullException.ThrowIfNull(output);

        string resolvedCorpusPath = Path.GetFullPath(corpusPath);
        FileStream corpusStream = File.OpenRead(resolvedCorpusPath);
        await using ConfiguredAsyncDisposable corpusStreamDisposal = corpusStream.ConfigureAwait(false);
        EvaluationCorpus? corpus = await JsonSerializer.DeserializeAsync<EvaluationCorpus>(
            corpusStream,
            InputOptions,
            cancellationToken).ConfigureAwait(false);
        Validate(corpus);

        List<EvaluationResult> results = [];
        foreach (EvaluationCase evaluationCase in corpus!.Cases) {
            var stopwatch = Stopwatch.StartNew();
            DevelopmentContext context = await queries.GetDevelopmentContextAsync(
                evaluationCase.Intent,
                evaluationCase.Query,
                evaluationCase.PlannedPath,
                baseRevision: null,
                headRevision: null,
                cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();

            bool scopeHit = evaluationCase.ExpectedPaths.Any(expected =>
                context.ExpandedScopePaths.Contains(expected, StringComparer.OrdinalIgnoreCase));
            bool sqlTopTenHit = context.SqlContextSearch?.Candidates
                .Take(10)
                .Any(candidate => evaluationCase.ExpectedPaths.Contains(
                    candidate.Path,
                    StringComparer.OrdinalIgnoreCase)) is true;
            bool expectedLayersPresent = evaluationCase.ExpectedLayers.All(expected =>
                context.EffectiveLayers.Contains(expected, StringComparer.OrdinalIgnoreCase));
            bool completeBundle = !context.PartialSuccess &&
                context.ChangeContext is not null &&
                context.TestPlan is not null;
            bool focusedChecksPresent = HasFocusedChecks(context.TestPlan);
            int compactCharacters = JsonSerializer.Serialize(context.ToCompact(), OutputOptions).Length;
            results.Add(new EvaluationResult(
                evaluationCase.Id,
                string.Equals(context.ContextRetrievalSource, "sqlite", StringComparison.Ordinal),
                context.ContextFallbackReason,
                scopeHit,
                sqlTopTenHit,
                completeBundle,
                focusedChecksPresent,
                expectedLayersPresent,
                context.ExpandedScopePaths.Count,
                compactCharacters,
                Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2, MidpointRounding.AwayFromZero),
                [.. context.ComponentErrors.Select(error => $"{error.Component}:{error.ErrorCode}")],
                [.. context.ExpandedScopePaths.Take(12)]));
        }

        double sqlitePrimaryRate = Rate(results.Count(result => result.SqlitePrimary), results.Count);
        double scopeRecallRate = Rate(results.Count(result => result.ScopeHit), results.Count);
        double sqlTopTenRecallRate = Rate(results.Count(result => result.SqlTopTenHit), results.Count);
        double completeBundleRate = Rate(results.Count(result => result.CompleteBundle), results.Count);
        double focusedChecksRate = Rate(results.Count(result => result.FocusedChecksPresent), results.Count);
        double expectedLayersRate = Rate(results.Count(result => result.ExpectedLayersPresent), results.Count);
        double averageExpandedScopePaths = Math.Round(
            results.Average(result => result.ExpandedScopePathCount),
            2,
            MidpointRounding.AwayFromZero);
        double[] durations = [.. results.Select(result => result.DurationMilliseconds).Order()];
        int p95Index = Math.Max(0, (int)Math.Ceiling(durations.Length * 0.95) - 1);
        double p95DurationMilliseconds = durations[p95Index];
        int maximumCompactCharacters = results.Max(result => result.CompactCharacters);
        bool passed = sqlitePrimaryRate >= corpus.Thresholds.MinimumSqlitePrimaryRate &&
            scopeRecallRate >= corpus.Thresholds.MinimumScopeRecallRate &&
            sqlTopTenRecallRate >= corpus.Thresholds.MinimumSqlTopTenRecallRate &&
            completeBundleRate >= corpus.Thresholds.MinimumCompleteBundleRate &&
            focusedChecksRate >= corpus.Thresholds.MinimumFocusedChecksRate &&
            expectedLayersRate >= corpus.Thresholds.MinimumExpectedLayersRate &&
            averageExpandedScopePaths <= corpus.Thresholds.MaximumAverageExpandedScopePaths &&
            p95DurationMilliseconds <= corpus.Thresholds.MaximumP95DurationMilliseconds &&
            maximumCompactCharacters <= corpus.Thresholds.MaximumCompactCharacters;
        var evaluation = new {
            schemaVersion = 1,
            corpusPath = resolvedCorpusPath.Replace('\\', '/'),
            passed,
            caseCount = results.Count,
            metrics = new {
                sqlitePrimaryRate,
                scopeRecallRate,
                sqlTopTenRecallRate,
                completeBundleRate,
                focusedChecksRate,
                expectedLayersRate,
                averageExpandedScopePaths,
                p95DurationMilliseconds,
                maximumCompactCharacters,
            },
            thresholds = corpus.Thresholds,
            failures = results.Where(result => !result.SqlitePrimary || !result.ScopeHit ||
                !result.SqlTopTenHit || !result.CompleteBundle || !result.FocusedChecksPresent ||
                !result.ExpectedLayersPresent).ToArray(),
            results,
        };
        string json = JsonSerializer.Serialize(evaluation, OutputOptions);
        await output.WriteAsync(json.AsMemory(), cancellationToken).ConfigureAwait(false);
        await output.WriteLineAsync().ConfigureAwait(false);
    }

    private static double Rate(int count, int total) => Math.Round(
        (double)count / total,
        4,
        MidpointRounding.AwayFromZero);

    private static bool HasFocusedChecks(WikiCommandResult? testPlan) {
        if (testPlan is null) {
            return false;
        }
        if (testPlan.RequiredChecks.Count > 0) {
            return true;
        }
        if (testPlan.StructuredOutput is not { ValueKind: JsonValueKind.Object } output) {
            return false;
        }
        string[] checkProperties = ["commands", "requiredChecks", "required", "recommended", "focusedTests"];
        return checkProperties.Any(name =>
            output.TryGetProperty(name, out JsonElement checks) &&
            checks.ValueKind == JsonValueKind.Array &&
            checks.GetArrayLength() > 0);
    }

    private static void Validate(EvaluationCorpus? corpus) {
        if (corpus is null || corpus.SchemaVersion != 1 || corpus.Cases.Length == 0) {
            throw new InvalidDataException("Unsupported development-context evaluation schema.");
        }
        if (corpus.Cases.Any(evaluationCase =>
            string.IsNullOrWhiteSpace(evaluationCase.Id) ||
            string.IsNullOrWhiteSpace(evaluationCase.Intent) ||
            string.IsNullOrWhiteSpace(evaluationCase.Query) ||
            string.IsNullOrWhiteSpace(evaluationCase.PlannedPath) ||
            evaluationCase.ExpectedPaths.Length == 0)) {
            throw new InvalidDataException("Development-context evaluation contains an invalid case.");
        }
        if (corpus.Cases.Select(evaluationCase => evaluationCase.Id)
            .Distinct(StringComparer.Ordinal)
            .Count() != corpus.Cases.Length) {
            throw new InvalidDataException("Development-context evaluation contains duplicate case ids.");
        }
    }

    private sealed record EvaluationCorpus(
        int SchemaVersion,
        EvaluationThresholds Thresholds,
        EvaluationCase[] Cases);

    private sealed record EvaluationThresholds(
        double MinimumSqlitePrimaryRate,
        double MinimumScopeRecallRate,
        double MinimumSqlTopTenRecallRate,
        double MinimumCompleteBundleRate,
        double MinimumFocusedChecksRate,
        double MinimumExpectedLayersRate,
        double MaximumAverageExpandedScopePaths,
        double MaximumP95DurationMilliseconds,
        int MaximumCompactCharacters);

    private sealed record EvaluationCase(
        string Id,
        string Intent,
        string Query,
        string PlannedPath,
        string[] ExpectedPaths,
        string[] ExpectedLayers);

    private sealed record EvaluationResult(
        string Id,
        bool SqlitePrimary,
        string? FallbackReason,
        bool ScopeHit,
        bool SqlTopTenHit,
        bool CompleteBundle,
        bool FocusedChecksPresent,
        bool ExpectedLayersPresent,
        int ExpandedScopePathCount,
        int CompactCharacters,
        double DurationMilliseconds,
        string[] ComponentErrors,
        string[] ExpandedScopePaths);
}
