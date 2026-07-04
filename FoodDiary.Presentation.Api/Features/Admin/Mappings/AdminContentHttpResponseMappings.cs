using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Common.Models;
using FoodDiary.Presentation.Api.Features.Admin.Responses;
using FoodDiary.Presentation.Api.Responses;

namespace FoodDiary.Presentation.Api.Features.Admin.Mappings;

public static class AdminContentHttpResponseMappings {
    public static AdminContentReportHttpResponse ToHttpResponse(this AdminContentReportModel model) {
        return new AdminContentReportHttpResponse(
            model.Id,
            model.ReporterId,
            model.TargetType,
            model.TargetId,
            model.Reason,
            model.Status,
            model.AdminNote,
            model.CreatedAtUtc,
            model.ReviewedAtUtc);
    }

    public static PagedHttpResponse<AdminContentReportHttpResponse> ToHttpResponse(
        this PagedResponse<AdminContentReportModel> response) {
        return response.ToPagedHttpResponse(ToHttpResponse);
    }
}
