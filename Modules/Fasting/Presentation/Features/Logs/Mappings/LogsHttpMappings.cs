using FoodDiary.Modules.Fasting.Application.Commands.RecordFastingTelemetry;
using FoodDiary.Modules.Fasting.Presentation.Features.Logs.Requests;

namespace FoodDiary.Modules.Fasting.Presentation.Features.Logs.Mappings;

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
