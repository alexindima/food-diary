namespace FoodDiary.Application.ContentReports.Models;

public sealed record ContentReportModel(
    Guid Id,
    Guid ReporterId,
    string TargetType,
    Guid TargetId,
    string Reason,
    string Status,
    string? AdminNote,
    DateTime CreatedAtUtc,
    DateTime? ReviewedAtUtc);
