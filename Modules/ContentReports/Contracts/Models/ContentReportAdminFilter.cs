namespace FoodDiary.Application.ContentReports.Models;

public sealed record ContentReportAdminFilter(DateTime? FromUtc = null, DateTime? ToUtc = null, string? TargetType = null,
    Guid? ReporterId = null, Guid? TargetId = null);
