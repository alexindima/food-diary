using System.Runtime.CompilerServices;
using System.Text.Json;
using FoodDiary.Development.Mcp.Protocol;

namespace FoodDiary.Development.Mcp.Wiki;

internal static class WikiContextSearchEvaluationRunner {
    private static readonly JsonSerializerOptions InputOptions = new(JsonSerializerDefaults.Web);
    private static readonly JsonSerializerOptions OutputOptions = new(JsonSerializerDefaults.Web) {
        WriteIndented = true,
    };

    public static async Task RunAsync(
        IWikiContextSearch search,
        string corpusPath,
        TextWriter output,
        CancellationToken cancellationToken,
        string? expectedChangeSetFingerprint = null) {
        ArgumentNullException.ThrowIfNull(search);
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
            WikiContextSearchResult searchResult = await search.SearchAsync(
                evaluationCase.Query,
                corpus.DiagnosticLimit,
                evaluationCase.ChangeType ?? "Any",
                module: null,
                scopePaths: null,
                cancellationToken,
                expectedChangeSetFingerprint).ConfigureAwait(false);
            int? rank = searchResult.Candidates
                .Where(candidate => evaluationCase.ExpectedPaths.Contains(
                    candidate.Path,
                    StringComparer.OrdinalIgnoreCase))
                .Select(candidate => (int?)candidate.Rank)
                .Order()
                .FirstOrDefault();
            results.Add(new EvaluationResult(
                evaluationCase.Id,
                rank,
                rank is null ? 0 : 1.0 / rank.Value,
                rank == 1,
                rank is not null and <= 10,
                searchResult.Ready,
                searchResult.QueryDurationMilliseconds,
                searchResult.UnavailableReason,
                [.. searchResult.Candidates.Take(5).Select(candidate => new EvaluationCandidate(
                    candidate.Rank,
                    candidate.Path,
                    candidate.Score))]));
        }

        int top1Count = results.Count(result => result.Top1);
        int top10Count = results.Count(result => result.Top10);
        double top1Rate = (double)top1Count / results.Count;
        double top10Rate = (double)top10Count / results.Count;
        double meanReciprocalRank = results.Average(result => result.ReciprocalRank);
        double[] durations = [.. results
            .Select(result => result.QueryDurationMilliseconds)
            .Order()];
        int p95Index = Math.Max(0, (int)Math.Ceiling(durations.Length * 0.95) - 1);
        bool passed = MeetsCriteria(
            corpus.Thresholds,
            results.Count,
            top1Rate,
            top10Rate,
            meanReciprocalRank);
        bool switchReady = MeetsCriteria(
            corpus.SwitchCriteria,
            results.Count,
            top1Rate,
            top10Rate,
            meanReciprocalRank);
        var evaluation = new {
            schemaVersion = 1,
            reader = "in-process-microsoft-data-sqlite",
            corpusPath = resolvedCorpusPath.Replace('\\', '/'),
            passed,
            switchReady,
            caseCount = results.Count,
            metrics = new {
                top1Count,
                top1Rate = Math.Round(top1Rate, 4, MidpointRounding.AwayFromZero),
                top10Count,
                top10Rate = Math.Round(top10Rate, 4, MidpointRounding.AwayFromZero),
                meanReciprocalRank = Math.Round(meanReciprocalRank, 4, MidpointRounding.AwayFromZero),
                averageQueryDurationMs = Math.Round(durations.Average(), 2, MidpointRounding.AwayFromZero),
                p95QueryDurationMs = Math.Round(durations[p95Index], 2, MidpointRounding.AwayFromZero),
            },
            thresholds = corpus.Thresholds,
            switchCriteria = corpus.SwitchCriteria,
            misses = results.Where(result => !result.Top10).ToArray(),
            results,
        };
        string json = JsonSerializer.Serialize(evaluation, OutputOptions);
        await output.WriteAsync(json.AsMemory(), cancellationToken).ConfigureAwait(false);
        await output.WriteLineAsync().ConfigureAwait(false);
    }

    private static void Validate(EvaluationCorpus? corpus) {
        if (corpus is null || corpus.SchemaVersion != 1) {
            throw new InvalidDataException("Unsupported context-search evaluation schema.");
        }
        if (corpus.DiagnosticLimit is < 10 or > 500 || corpus.Cases.Length == 0) {
            throw new InvalidDataException("Context-search evaluation corpus is invalid.");
        }
        if (corpus.Cases.Any(evaluationCase =>
            string.IsNullOrWhiteSpace(evaluationCase.Id) ||
            string.IsNullOrWhiteSpace(evaluationCase.Query) ||
            evaluationCase.ExpectedPaths.Length == 0)) {
            throw new InvalidDataException("Context-search evaluation contains an invalid case.");
        }
        if (corpus.Cases.Select(evaluationCase => evaluationCase.Id).Distinct(StringComparer.Ordinal).Count() !=
            corpus.Cases.Length) {
            throw new InvalidDataException("Context-search evaluation contains duplicate case ids.");
        }
    }

    private static bool MeetsCriteria(
        EvaluationCriteria criteria,
        int caseCount,
        double top1Rate,
        double top10Rate,
        double meanReciprocalRank) =>
        caseCount >= criteria.MinimumCaseCount &&
        top1Rate >= criteria.MinimumTop1Rate &&
        top10Rate >= criteria.MinimumTop10Rate &&
        meanReciprocalRank >= criteria.MinimumMeanReciprocalRank;

    private sealed record EvaluationCorpus(
        int SchemaVersion,
        int DiagnosticLimit,
        EvaluationCriteria Thresholds,
        EvaluationCriteria SwitchCriteria,
        EvaluationCase[] Cases);

    private sealed record EvaluationCriteria(
        int MinimumCaseCount,
        double MinimumTop1Rate,
        double MinimumTop10Rate,
        double MinimumMeanReciprocalRank);

    private sealed record EvaluationCase(
        string Id,
        string Query,
        string? ChangeType,
        string[] ExpectedPaths);

    private sealed record EvaluationResult(
        string Id,
        int? Rank,
        double ReciprocalRank,
        bool Top1,
        bool Top10,
        bool Ready,
        double QueryDurationMilliseconds,
        string? UnavailableReason,
        EvaluationCandidate[] TopCandidates);

    private sealed record EvaluationCandidate(int Rank, string Path, int Score);
}
