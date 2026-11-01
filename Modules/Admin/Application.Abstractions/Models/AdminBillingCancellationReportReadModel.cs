namespace FoodDiary.Modules.Admin.Application.Abstractions.Models;

public sealed record AdminBillingCancellationReportReadModel(
    IReadOnlyList<AdminBillingCancellationReadModel> Items, int Page, int Limit, int TotalItems,
    int TotalPages, int FeedbackCount, IReadOnlyList<AdminBillingCancellationReasonReadModel> Reasons);
