namespace FoodDiary.Modules.Admin.Presentation.Responses;

public sealed record AdminBillingCancellationReportHttpResponse(
    IReadOnlyList<AdminBillingCancellationHttpResponse> Data, int Page, int Limit, int TotalItems,
    int TotalPages, int FeedbackCount, IReadOnlyList<AdminBillingCancellationReasonHttpResponse> Reasons);
