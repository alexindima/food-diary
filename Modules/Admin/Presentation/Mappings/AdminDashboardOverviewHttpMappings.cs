using FoodDiary.Modules.Admin.Application.Models;
using FoodDiary.Modules.Admin.Application.Queries.GetAdminDashboardOverview;
using FoodDiary.Modules.Admin.Presentation.Requests;
using FoodDiary.Modules.Admin.Presentation.Responses;

namespace FoodDiary.Modules.Admin.Presentation.Mappings;

public static class AdminDashboardOverviewHttpMappings {
    public static GetAdminDashboardOverviewQuery ToQuery(this GetAdminDashboardOverviewHttpQuery query) => new(query.From, query.To, query.AllTime);

    public static AdminDashboardOverviewHttpResponse ToHttpResponse(this AdminDashboardOverviewModel model) =>
        new(model.FromUtc, model.ToUtc, model.Interval, MapPeriod(model.Period), model.Previous is null ? null : MapPeriod(model.Previous),
            model.TotalUsersNow, model.PremiumUsersNow, model.PendingReportsNow);

    private static AdminDashboardPeriodHttpResponse MapPeriod(AdminDashboardPeriodModel model) => new(model.FromUtc, model.ToUtc,
        new AdminDashboardMetricsHttpResponse(model.Metrics.Registrations, model.Metrics.PayingUsers, model.Metrics.AiTokens,
            [.. model.Metrics.Trend.Select(point => new AdminDashboardTrendHttpResponse(point.Date, point.Registrations, point.AiTokens,
                [.. point.Revenue.Select(revenue => new AdminDashboardRevenuePointHttpResponse(revenue.Currency, revenue.Gross))]))]),
        [.. model.Currencies.Select(currency => new AdminBillingRevenueCurrencyHttpResponse(currency.Currency, currency.Gross,
            currency.Refunds, currency.Chargebacks, currency.Reversals, currency.Net, currency.SuccessfulPayments,
            currency.Tax, currency.PaddleFees, currency.PaddleEarnings, currency.EarningsTrackedPayments))]);
}
