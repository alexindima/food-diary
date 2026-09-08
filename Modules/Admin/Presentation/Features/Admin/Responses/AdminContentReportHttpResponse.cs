namespace FoodDiary.Presentation.Api.Features.Admin.Responses;

public sealed record AdminContentReportHttpResponse(
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
