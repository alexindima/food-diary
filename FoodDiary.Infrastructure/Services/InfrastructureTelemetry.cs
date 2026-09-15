using System.Diagnostics.Metrics;

namespace FoodDiary.Infrastructure.Services;

internal static class InfrastructureTelemetry {
    public const string MeterName = "FoodDiary.Infrastructure";

    private static readonly Meter Meter = new(MeterName);

    public static readonly Counter<long> AiRequestCounter = Meter.CreateCounter<long>(
        "fooddiary.ai.requests");

    public static readonly Counter<long> AiQuotaRejectionCounter = Meter.CreateCounter<long>(
        "fooddiary.ai.quota_rejections");

    public static readonly Counter<long> AiFallbackCounter = Meter.CreateCounter<long>(
        "fooddiary.ai.fallbacks");

    public static readonly Counter<long> DatabaseCommandFailureCounter = Meter.CreateCounter<long>(
        "fooddiary.db.command.failures");

    public static readonly Counter<long> StorageOperationCounter = Meter.CreateCounter<long>(
        "fooddiary.storage.operations");

    internal static void RecordDatabaseCommandFailure(string operation, string source, string errorType) {
        DatabaseCommandFailureCounter.Add(
            1,
            new KeyValuePair<string, object?>("db.system", "postgresql"),
            new KeyValuePair<string, object?>("fooddiary.db.operation", operation),
            new KeyValuePair<string, object?>("fooddiary.db.source", source),
            new KeyValuePair<string, object?>("error.type", errorType));
    }

    internal static void RecordStorageOperation(string operation, string outcome, string? errorType = null) {
        StorageOperationCounter.Add(
            1,
            new KeyValuePair<string, object?>("fooddiary.storage.operation", operation),
            new KeyValuePair<string, object?>("fooddiary.storage.outcome", outcome),
            new KeyValuePair<string, object?>("error.type", errorType));
    }
}
