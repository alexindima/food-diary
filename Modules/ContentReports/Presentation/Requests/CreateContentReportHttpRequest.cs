namespace FoodDiary.Modules.ContentReports.Presentation.Requests;

public sealed record CreateContentReportHttpRequest(
    string TargetType,
    Guid TargetId,
    string Reason);
