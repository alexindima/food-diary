namespace FoodDiary.Presentation.Api.Features.ContentReports.Requests;

public sealed record CreateContentReportHttpRequest(
    string TargetType,
    Guid TargetId,
    string Reason);
