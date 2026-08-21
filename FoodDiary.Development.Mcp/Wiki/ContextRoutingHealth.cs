namespace FoodDiary.Development.Mcp.Wiki;

public sealed record ContextRoutingHealth(
    int SampleCount,
    int SqlitePrimaryCount,
    int JsonFallbackCount,
    double JsonFallbackRate,
    int SqliteNoCandidateFallbackCount,
    double P50Milliseconds,
    double P95Milliseconds,
    DateTimeOffset? OldestSampleAtUtc,
    DateTimeOffset? LatestSampleAtUtc,
    IReadOnlyDictionary<string, int> FallbackReasonCounts,
    int RetentionLimit,
    int MinimumRetirementSamples,
    bool JsonFallbackRetirementReady,
    bool PersistenceHealthy,
    long PersistenceFailures,
    DateTimeOffset? LastPersistenceFailureAtUtc) {
    public static ContextRoutingHealth Empty { get; } = new(
        SampleCount: 0,
        SqlitePrimaryCount: 0,
        JsonFallbackCount: 0,
        JsonFallbackRate: 0,
        SqliteNoCandidateFallbackCount: 0,
        P50Milliseconds: 0,
        P95Milliseconds: 0,
        OldestSampleAtUtc: null,
        LatestSampleAtUtc: null,
        FallbackReasonCounts: new Dictionary<string, int>(StringComparer.Ordinal),
        RetentionLimit: 0,
        MinimumRetirementSamples: 100,
        JsonFallbackRetirementReady: false,
        PersistenceHealthy: true,
        PersistenceFailures: 0,
        LastPersistenceFailureAtUtc: null);
}
