namespace FoodDiary.Modules.Admin.Presentation.Responses;

public sealed record AdminDashboardPeriodHttpResponse(DateTime FromUtc, DateTime ToUtc,
    AdminDashboardMetricsHttpResponse Metrics, IReadOnlyList<AdminBillingRevenueCurrencyHttpResponse> Currencies);
