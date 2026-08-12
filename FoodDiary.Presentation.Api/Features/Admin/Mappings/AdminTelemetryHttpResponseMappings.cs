using FoodDiary.Application.Abstractions.Fasting.Models;
using FoodDiary.Presentation.Api.Features.Admin.Responses;

namespace FoodDiary.Presentation.Api.Features.Admin.Mappings;

public static class AdminTelemetryHttpResponseMappings {
    extension(FastingTelemetrySummaryModel summary) {
        public FastingTelemetrySummaryHttpResponse ToHttpResponse() {
            return new FastingTelemetrySummaryHttpResponse(
                summary.WindowHours,
                summary.GeneratedAtUtc,
                summary.StartedSessions,
                summary.CompletedSessions,
                summary.SavedCheckIns,
                summary.ReminderPresetSelections,
                summary.ReminderTimingSaves,
                summary.PresetReminderTimingSaves,
                summary.ManualReminderTimingSaves,
                summary.CompletionRatePercent,
                summary.CheckInRatePercent,
                summary.AverageCompletedDurationHours,
                summary.LastCheckInAtUtc,
                summary.LastEventAtUtc,
                summary.TopPresets.Select(x => new FastingTelemetryPresetHttpResponse(
                    x.PresetId,
                    x.SelectionCount,
                    x.TimingSaveCount,
                    x.FirstReminderHours,
                    x.FollowUpReminderHours,
                    x.StartedSessions,
                    x.CompletedSessions,
                    x.SavedCheckIns,
                    x.CompletionRatePercent,
                    x.CheckInRatePercent)).ToList());
        }
    }
}
