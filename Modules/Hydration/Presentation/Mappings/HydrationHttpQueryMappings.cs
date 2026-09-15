using FoodDiary.Modules.Hydration.Application.Queries.GetHydrationDailyTotal;
using FoodDiary.Modules.Hydration.Application.Queries.GetHydrationEntries;
using FoodDiary.Modules.Hydration.Presentation.Requests;

namespace FoodDiary.Modules.Hydration.Presentation.Mappings;

public static class HydrationHttpQueryMappings {
    extension(GetHydrationEntriesHttpQuery query) {
        public GetHydrationEntriesQuery ToEntriesQuery(Guid userId, DateTime utcNow) {
            return new GetHydrationEntriesQuery(userId, query.DateUtc ?? utcNow);
        }
        public GetHydrationDailyTotalQuery ToDailyQuery(Guid userId, DateTime utcNow) {
            return new GetHydrationDailyTotalQuery(userId, query.DateUtc ?? utcNow);
        }
    }
}
