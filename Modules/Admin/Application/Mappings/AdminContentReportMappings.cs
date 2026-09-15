using FoodDiary.Modules.ContentReports.Contracts.Models;
using FoodDiary.Modules.Admin.Application.Models;

namespace FoodDiary.Modules.Admin.Application.Mappings;

public static class AdminContentReportMappings {
    public static AdminContentReportModel ToAdminModel(this ContentReportAdminReadModel report) =>
        new(
            report.Id,
            report.UserId,
            report.TargetType,
            report.TargetId,
            report.Reason,
            report.Status,
            report.AdminNote,
            report.CreatedOnUtc,
            report.ReviewedAtUtc, report.ReviewedByUserId, report.TargetTitle, report.TargetExcerpt);
}
