using System.Diagnostics.Metrics;

namespace FoodDiary.Integrations.Services;

internal static class IntegrationsTelemetry {
    public const string MeterName = "FoodDiary.Integrations";

    private static readonly Meter Meter = new(MeterName);

    public static readonly Counter<long> AiRequestCounter = Meter.CreateCounter<long>(
        "fooddiary.ai.requests");

    public static readonly Counter<long> AiFallbackCounter = Meter.CreateCounter<long>(
        "fooddiary.ai.fallbacks");

    public static readonly Counter<long> StorageOperationCounter = Meter.CreateCounter<long>(
        "fooddiary.storage.operations");

    public static readonly Counter<long> ExternalProviderRequestCounter = Meter.CreateCounter<long>(
        "fooddiary.external_provider.requests");

    public static readonly Histogram<double> ExternalProviderDuration = Meter.CreateHistogram<double>(
        "fooddiary.external_provider.duration",
        unit: "ms");

    public static void RecordStorageOperation(string operation, string outcome, string? errorType = null) {
        StorageOperationCounter.Add(
            1,
            new KeyValuePair<string, object?>("fooddiary.storage.operation", operation),
            new KeyValuePair<string, object?>("fooddiary.storage.outcome", outcome),
            new KeyValuePair<string, object?>("error.type", errorType));
    }

    public static void RecordExternalProviderRequest(
        string provider,
        string operation,
        string outcome,
        double durationMs,
        string? errorType = null) {
        KeyValuePair<string, object?>[] tags = [
            new("fooddiary.external_provider", provider),
            new("fooddiary.external_provider.operation", operation),
            new("fooddiary.external_provider.outcome", outcome),
            new("error.type", errorType),
        ];

        ExternalProviderRequestCounter.Add(1, tags);
        ExternalProviderDuration.Record(durationMs, tags);
    }
}
