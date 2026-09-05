namespace FoodDiary.Presentation.Api.Features.Admin.Responses;

public sealed record AdminBillingRevenueSummaryHttpResponse(
    DateTime FromUtc,
    DateTime ToUtc,
    IReadOnlyList<AdminBillingRevenueCurrencyHttpResponse> Currencies);
