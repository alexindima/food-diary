namespace FoodDiary.Modules.Admin.Application.Models;

public sealed record AdminContentReportModel(
    Guid Id,
    Guid ReporterId,
    string TargetType,
    Guid TargetId,
    string Reason,
    string Status,
    string? AdminNote,
    DateTime CreatedAtUtc,
    DateTime? ReviewedAtUtc,
    Guid? ReviewedByUserId = null, string? TargetTitle = null, string? TargetExcerpt = null);
