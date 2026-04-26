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

    public static void RecordStorageOperation(string operation, string outcome, string? errorType = null) {
        StorageOperationCounter.Add(
            1,
            new KeyValuePair<string, object?>("fooddiary.storage.operation", operation),
            new KeyValuePair<string, object?>("fooddiary.storage.outcome", outcome),
            new KeyValuePair<string, object?>("error.type", errorType));
    }
}
