using FoodDiary.Application.ContentReports.Models;
using FoodDiary.Application.Admin.Models;

namespace FoodDiary.Application.Admin.Mappings;

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
