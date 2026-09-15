using FoodDiary.Modules.ContentReports.Application.Models;
using FoodDiary.Modules.ContentReports.Application.Commands.CreateContentReport;
using FoodDiary.Modules.ContentReports.Presentation.Requests;
using FoodDiary.Modules.ContentReports.Presentation.Responses;

namespace FoodDiary.Modules.ContentReports.Presentation.Mappings;

public static class ContentReportHttpMappings {
    extension(CreateContentReportHttpRequest request) {
        public CreateContentReportCommand ToCommand(
        Guid userId) =>
                new(userId, request.TargetType, request.TargetId, request.Reason);
    }

    extension(ContentReportModel model) {
        public ContentReportHttpResponse ToHttpResponse() =>
                new(model.Id, model.ReporterId, model.TargetType, model.TargetId,
                    model.Reason, model.Status, model.AdminNote,
                    model.CreatedAtUtc, model.ReviewedAtUtc);
    }
}
