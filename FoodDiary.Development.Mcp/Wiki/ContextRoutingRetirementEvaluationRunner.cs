using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using FoodDiary.Development.Mcp.Protocol;

namespace FoodDiary.Development.Mcp.Wiki;

internal static class ContextRoutingRetirementEvaluationRunner {
    private static readonly JsonSerializerOptions InputOptions = new(JsonSerializerDefaults.Web);
    private static readonly JsonSerializerOptions OutputOptions = new(JsonSerializerDefaults.Web) {
        WriteIndented = true,
    };

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public static async Task RunAsync(
        WikiQueryService queries,
        ContextRoutingTelemetryStore routingStore,
        string corpusPath,
        TextWriter output,
        CancellationToken cancellationToken,
        int? maximumCases = null) {
        ArgumentNullException.ThrowIfNull(queries);
        ArgumentNullException.ThrowIfNull(routingStore);
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
        if (maximumCases is <= 0) {
            throw new ArgumentOutOfRangeException(nameof(maximumCases), "Maximum cases must be positive.");
        }

        ContextRoutingHealth before = routingStore.Capture();
        List<EvaluationResult> results = [];
        foreach (EvaluationCase evaluationCase in corpus!.Cases.Take(maximumCases ?? int.MaxValue)) {
            var stopwatch = Stopwatch.StartNew();
            DevelopmentContext context = await queries.GetDevelopmentContextAsync(
                intent: evaluationCase.Query,
                query: evaluationCase.Query,
                plannedPath: null,
                baseRevision: null,
                headRevision: null,
                cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();

            bool expectedPathInTopTen = context.SqlContextSearch?.Candidates
                .Take(10)
                .Any(candidate => evaluationCase.ExpectedPaths.Contains(
                    candidate.Path,
                    StringComparer.OrdinalIgnoreCase)) is true;
            results.Add(new EvaluationResult(
                evaluationCase.Id,
                string.Equals(context.ContextRetrievalSource, "sqlite", StringComparison.Ordinal),
                context.ContextFallbackReason,
                expectedPathInTopTen,
                !context.PartialSuccess && context.ChangeContext is not null && context.TestPlan is not null,
                Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2, MidpointRounding.AwayFromZero),
                [.. context.ComponentErrors.Select(error => $"{error.Component}:{error.ErrorCode}")]));
        }

        ContextRoutingHealth after = routingStore.Capture();
        bool passed = results.All(result =>
            result.SqlitePrimary &&
            result.ExpectedPathInTopTen &&
            result.CompleteBundle);
        var evaluation = new {
            schemaVersion = 1,
            corpusPath = resolvedCorpusPath.Replace('\\', '/'),
            passed,
            caseCount = results.Count,
            metrics = new {
                sqlitePrimaryCount = results.Count(result => result.SqlitePrimary),
                expectedPathTopTenCount = results.Count(result => result.ExpectedPathInTopTen),
                completeBundleCount = results.Count(result => result.CompleteBundle),
                p95DurationMilliseconds = Percentile95(results.Select(result => result.DurationMilliseconds)),
            },
            persistentEvidenceBefore = before,
            persistentEvidenceAfter = after,
            failures = results.Where(result =>
                !result.SqlitePrimary ||
                !result.ExpectedPathInTopTen ||
                !result.CompleteBundle).ToArray(),
            results,
        };
        string json = JsonSerializer.Serialize(evaluation, OutputOptions);
        await output.WriteAsync(json.AsMemory(), cancellationToken).ConfigureAwait(false);
        await output.WriteLineAsync().ConfigureAwait(false);

        if (!passed) {
            throw new InvalidOperationException(
                "Context-routing retirement evaluation observed a fallback, top-ten miss, or incomplete bundle.");
        }
    }

    private static double Percentile95(IEnumerable<double> values) {
        double[] sorted = [.. values.Order()];
        int index = Math.Max(0, (int)Math.Ceiling(sorted.Length * 0.95) - 1);
        return sorted[index];
    }

    private static void Validate(EvaluationCorpus? corpus) {
        if (corpus is null || corpus.SchemaVersion != 1 || corpus.Cases.Length == 0) {
            throw new InvalidDataException("Unsupported context-routing retirement corpus schema.");
        }
        if (corpus.Cases.Any(evaluationCase =>
            string.IsNullOrWhiteSpace(evaluationCase.Id) ||
            string.IsNullOrWhiteSpace(evaluationCase.Query) ||
            evaluationCase.ExpectedPaths.Length == 0)) {
            throw new InvalidDataException("Context-routing retirement corpus contains an invalid case.");
        }
        if (corpus.Cases.Select(evaluationCase => evaluationCase.Id)
            .Distinct(StringComparer.Ordinal)
            .Count() != corpus.Cases.Length) {
            throw new InvalidDataException("Context-routing retirement corpus contains duplicate case ids.");
        }
    }

    private sealed record EvaluationCorpus(int SchemaVersion, EvaluationCase[] Cases);

    private sealed record EvaluationCase(string Id, string Query, string[] ExpectedPaths);

    private sealed record EvaluationResult(
        string Id,
        bool SqlitePrimary,
        string? FallbackReason,
        bool ExpectedPathInTopTen,
        bool CompleteBundle,
        double DurationMilliseconds,
        string[] ComponentErrors);
}
