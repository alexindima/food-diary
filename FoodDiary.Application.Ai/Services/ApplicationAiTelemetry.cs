using System.Diagnostics.Metrics;
using FoodDiary.Application.Abstractions.Ai.Common;

namespace FoodDiary.Application.Ai.Services;

internal static class ApplicationAiTelemetry {
    public const string MeterName = "FoodDiary.Application.Ai";

    private static readonly Meter Meter = new(MeterName);

    public static readonly Counter<long> QuotaRejectionCounter = Meter.CreateCounter<long>(
        "fooddiary.ai.quota_rejections");

    public static readonly Counter<long> QuotaReservationCounter = Meter.CreateCounter<long>(
        "fooddiary.ai.quota_reservations");

    public static readonly Counter<long> QuotaReconciliationCounter = Meter.CreateCounter<long>(
        "fooddiary.ai.quota_reconciliations");

    public static void RecordQuotaRejection(string operation) {
        QuotaRejectionCounter.Add(
            1,
            new KeyValuePair<string, object?>("fooddiary.ai.operation", operation));
    }

    public static void RecordQuotaReservation(string operation, AiQuotaReservationStatus status) {
        QuotaReservationCounter.Add(
            1,
            new KeyValuePair<string, object?>("fooddiary.ai.operation", operation),
            new KeyValuePair<string, object?>("fooddiary.ai.reservation_status", status.ToString().ToLowerInvariant()));
    }

    public static void RecordQuotaReconciliation(string operation, string usageKind) {
        QuotaReconciliationCounter.Add(
            1,
            new KeyValuePair<string, object?>("fooddiary.ai.operation", operation),
            new KeyValuePair<string, object?>("fooddiary.ai.usage_kind", usageKind));
    }
}
