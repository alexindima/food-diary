namespace FoodDiary.Presentation.Api.Features.ContentReports.Responses;

public sealed record ContentReportHttpResponse(
    Guid Id,
    Guid ReporterId,
    string TargetType,
    Guid TargetId,
    string Reason,
    string Status,
    string? AdminNote,
    DateTime CreatedAtUtc,
    DateTime? ReviewedAtUtc);
