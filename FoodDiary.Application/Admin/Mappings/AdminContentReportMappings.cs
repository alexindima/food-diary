using FoodDiary.Application.Admin.Models;
using FoodDiary.Domain.Entities.Social;

namespace FoodDiary.Application.Admin.Mappings;

public static class AdminContentReportMappings {
    public static AdminContentReportModel ToAdminModel(this ContentReport report) =>
        new(
            report.Id.Value,
            report.UserId.Value,
            report.TargetType.ToString(),
            report.TargetId,
            report.Reason,
            report.Status.ToString(),
            report.AdminNote,
            report.CreatedOnUtc,
            report.ReviewedAtUtc);
}
