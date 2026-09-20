using FoodDiary.Modules.DailyAdvices.Contracts.Queries.GetDailyAdvice;
using FoodDiary.Modules.Dashboard.Application.Commands.SendDashboardTestEmail;
using FoodDiary.Modules.Dashboard.Application.Queries.GetDashboardSnapshot;
using FoodDiary.Modules.Dashboard.Presentation.Requests;

namespace FoodDiary.Modules.Dashboard.Presentation.Mappings;

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
                query.TimeZoneOffsetMinutes,
                query.TimeZoneId);
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
