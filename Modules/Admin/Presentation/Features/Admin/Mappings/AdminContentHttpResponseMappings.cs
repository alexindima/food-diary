using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Abstractions.Common.Models;
using FoodDiary.Presentation.Api.Features.Admin.Responses;
using FoodDiary.Presentation.Api.Responses;

namespace FoodDiary.Presentation.Api.Features.Admin.Mappings;

public static class AdminContentHttpResponseMappings {
    extension(AdminContentReportModel model) {
        public AdminContentReportHttpResponse ToHttpResponse() {
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
    }

    extension(PagedResponse<AdminContentReportModel> response) {
        public PagedHttpResponse<AdminContentReportHttpResponse> ToHttpResponse(
        ) {
            return response.ToPagedHttpResponse(ToHttpResponse);
        }
    }
}
