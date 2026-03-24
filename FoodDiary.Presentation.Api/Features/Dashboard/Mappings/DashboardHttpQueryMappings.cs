using FoodDiary.Application.DailyAdvices.Queries.GetDailyAdvice;
using FoodDiary.Application.Dashboard.Queries.GetDashboardSnapshot;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Presentation.Api.Features.Dashboard.Requests;

namespace FoodDiary.Presentation.Api.Features.Dashboard.Mappings;

public static class DashboardHttpQueryMappings {
    public static GetDashboardSnapshotQuery ToQuery(this GetDashboardSnapshotHttpQuery query, UserId userId) {
        return new GetDashboardSnapshotQuery(userId, query.Date, query.Page, query.PageSize, query.Locale, query.TrendDays);
    }

    public static GetDailyAdviceQuery ToQuery(this GetDailyAdviceHttpQuery query, UserId userId) {
        return new GetDailyAdviceQuery(userId, query.Date, query.Locale);
    }
}
