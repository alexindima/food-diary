using FoodDiary.Application.ContentReports.Commands.CreateContentReport;
using FoodDiary.Application.ContentReports.Models;
using FoodDiary.Presentation.Api.Features.ContentReports.Requests;
using FoodDiary.Presentation.Api.Features.ContentReports.Responses;

namespace FoodDiary.Presentation.Api.Features.ContentReports.Mappings;

public static class ContentReportHttpMappings {
    public static CreateContentReportCommand ToCommand(
        this CreateContentReportHttpRequest request, Guid userId) =>
        new(userId, request.TargetType, request.TargetId, request.Reason);

    public static ContentReportHttpResponse ToHttpResponse(this ContentReportModel model) =>
        new(model.Id, model.ReporterId, model.TargetType, model.TargetId,
            model.Reason, model.Status, model.AdminNote,
            model.CreatedAtUtc, model.ReviewedAtUtc);
}
