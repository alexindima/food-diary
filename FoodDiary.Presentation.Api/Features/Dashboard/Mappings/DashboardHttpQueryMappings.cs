using FoodDiary.Application.DailyAdvices.Queries.GetDailyAdvice;
using FoodDiary.Application.Dashboard.Commands.SendDashboardTestEmail;
using FoodDiary.Application.Dashboard.Queries.GetDashboardSnapshot;
using FoodDiary.Presentation.Api.Features.Dashboard.Requests;

namespace FoodDiary.Presentation.Api.Features.Dashboard.Mappings;

public static class DashboardHttpQueryMappings {
    extension(GetDashboardSnapshotHttpQuery query) {
        public GetDashboardSnapshotQuery ToQuery(Guid userId) {
            return new GetDashboardSnapshotQuery(
                userId,
                query.Date,
                query.Page,
                query.PageSize,
                query.Locale,
                query.TrendDays,
                query.TimeZoneOffsetMinutes);
        }
    }

    extension(GetDailyAdviceHttpQuery query) {
        public GetDailyAdviceQuery ToQuery(Guid userId) {
            return new GetDailyAdviceQuery(userId, query.Date, query.Locale);
        }
    }

    extension(Guid userId) {
        public SendDashboardTestEmailCommand ToTestEmailCommand() {
            return new SendDashboardTestEmailCommand(userId);
        }
    }
}
