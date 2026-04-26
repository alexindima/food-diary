using System.Diagnostics.Metrics;

namespace FoodDiary.Application.Email.Common;

internal static class ApplicationEmailTelemetry {
    public const string MeterName = "FoodDiary.Application.Email";

    private static readonly Meter Meter = new(MeterName);

    public static readonly Counter<long> DispatchCounter = Meter.CreateCounter<long>(
        "fooddiary.email.dispatch.events");

    public static void RecordEmailDispatch(string template, string locale, string outcome, string? errorType = null) {
        DispatchCounter.Add(
            1,
            new KeyValuePair<string, object?>("fooddiary.email.template", template),
            new KeyValuePair<string, object?>("fooddiary.email.locale", locale),
            new KeyValuePair<string, object?>("fooddiary.email.outcome", outcome),
            new KeyValuePair<string, object?>("error.type", errorType));
    }
}
