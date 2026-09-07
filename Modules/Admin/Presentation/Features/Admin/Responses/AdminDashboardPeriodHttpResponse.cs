namespace FoodDiary.Presentation.Api.Features.Admin.Responses;

public sealed record AdminDashboardPeriodHttpResponse(DateTime FromUtc, DateTime ToUtc,
    AdminDashboardMetricsHttpResponse Metrics, IReadOnlyList<AdminBillingRevenueCurrencyHttpResponse> Currencies);
