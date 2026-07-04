using System.Globalization;
using System.Text.Json;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Fasting.Common;
using FoodDiary.Application.Common.Abstractions.Messaging;

namespace FoodDiary.Application.Fasting.Commands.RecordFastingTelemetry;

public sealed class RecordFastingTelemetryCommandHandler(
    IFastingTelemetryEventWriteRepository repository,
    TimeProvider timeProvider)
    : ICommandHandler<RecordFastingTelemetryCommand, Result> {
    public async Task<Result> Handle(RecordFastingTelemetryCommand command, CancellationToken cancellationToken) {
        if (!string.Equals(command.Category, "user_action", StringComparison.OrdinalIgnoreCase) ||
            !command.Name.StartsWith("fasting.", StringComparison.OrdinalIgnoreCase)) {
            return Result.Success();
        }

        IReadOnlyDictionary<string, string> details = ReadDetails(command.Details);
        var record = new FastingTelemetryEventRecord(
            command.Name,
            ParseTimestampUtc(command.Timestamp),
            ReadString(details, "sessionId"),
            ReadString(details, "protocol"),
            ReadString(details, "planType"),
            ReadString(details, "status"),
            ReadString(details, "occurrenceKind"),
            ReadString(details, "reminderPresetId") ?? ReadString(details, "presetId"),
            ReadString(details, "source"),
            ReadInt(details, "firstReminderHours"),
            ReadInt(details, "followUpReminderHours"),
            ReadInt(details, "plannedDurationHours"),
            ReadDouble(details, "actualDurationHours"),
            ReadInt(details, "hungerLevel"),
            ReadInt(details, "energyLevel"),
            ReadInt(details, "moodLevel"),
            ReadInt(details, "symptomsCount"),
            ReadBool(details, "hadNotes"));

        await repository.AddAsync(record, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    private DateTime ParseTimestampUtc(string? timestamp) {
        return DateTime.TryParse(
            timestamp,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out DateTime parsed)
            ? parsed
            : timeProvider.GetUtcNow().UtcDateTime;
    }

    private static IReadOnlyDictionary<string, string> ReadDetails(JsonElement? details) {
        if (details is null || details.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined || details.Value.ValueKind != JsonValueKind.Object) {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (JsonProperty property in details.Value.EnumerateObject()) {
            string? value = property.Value.ValueKind switch {
                JsonValueKind.String => property.Value.GetString(),
                JsonValueKind.Number => property.Value.GetRawText(),
                JsonValueKind.True => bool.TrueString,
                JsonValueKind.False => bool.FalseString,
                _ => null,
            };

            if (!string.IsNullOrWhiteSpace(value)) {
                result[property.Name] = value;
            }
        }

        return result;
    }

    private static string? ReadString(IReadOnlyDictionary<string, string> details, string key) =>
        details.GetValueOrDefault(key);

    private static int? ReadInt(IReadOnlyDictionary<string, string> details, string key) =>
        details.TryGetValue(key, out string? value) && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
            ? parsed
            : null;

    private static double? ReadDouble(IReadOnlyDictionary<string, string> details, string key) =>
        details.TryGetValue(key, out string? value) && double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed)
            ? parsed
            : null;

    private static bool? ReadBool(IReadOnlyDictionary<string, string> details, string key) =>
        details.TryGetValue(key, out string? value) && bool.TryParse(value, out bool parsed)
            ? parsed
            : null;
}
