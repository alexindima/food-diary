using FoodDiary.Modules.Admin.Application.Models;
using FoodDiary.Application.Abstractions.Common.Models;
using FoodDiary.Modules.Admin.Presentation.Features.Admin.Responses;
using FoodDiary.Presentation.Api.Responses;

namespace FoodDiary.Modules.Admin.Presentation.Features.Admin.Mappings;

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
                model.ReviewedAtUtc, model.ReviewedByUserId, model.TargetTitle, model.TargetExcerpt);
        }
    }

    extension(PagedResponse<AdminContentReportModel> response) {
        public PagedHttpResponse<AdminContentReportHttpResponse> ToHttpResponse(
        ) {
            return response.ToPagedHttpResponse(ToHttpResponse);
        }
    }
}
