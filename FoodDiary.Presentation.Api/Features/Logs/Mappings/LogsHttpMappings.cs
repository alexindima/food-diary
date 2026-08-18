using FoodDiary.Application.Fasting.Commands.RecordFastingTelemetry;
using FoodDiary.Presentation.Api.Features.Logs.Requests;

namespace FoodDiary.Presentation.Api.Features.Logs.Mappings;

public static class LogsHttpMappings {
    extension(ClientTelemetryLogHttpRequest request) {
        public RecordFastingTelemetryCommand ToFastingTelemetryCommand() {
            return new RecordFastingTelemetryCommand(
                request.Category,
                request.Name,
                request.Timestamp,
                request.Details);
        }
    }
}
