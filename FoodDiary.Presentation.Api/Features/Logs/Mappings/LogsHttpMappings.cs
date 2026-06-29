using FoodDiary.Application.Fasting.Commands.RecordFastingTelemetry;
using FoodDiary.Presentation.Api.Features.Logs.Requests;

namespace FoodDiary.Presentation.Api.Features.Logs.Mappings;

public static class LogsHttpMappings {
    public static RecordFastingTelemetryCommand ToCommand(this ClientTelemetryLogHttpRequest request) {
        return new RecordFastingTelemetryCommand(
            request.Category,
            request.Name,
            request.Timestamp,
            request.Details);
    }
}
