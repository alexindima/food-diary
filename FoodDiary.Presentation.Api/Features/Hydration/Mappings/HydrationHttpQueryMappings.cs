using FoodDiary.Application.Hydration.Queries.GetHydrationDailyTotal;
using FoodDiary.Application.Hydration.Queries.GetHydrationEntries;
using FoodDiary.Presentation.Api.Features.Hydration.Requests;

namespace FoodDiary.Presentation.Api.Features.Hydration.Mappings;

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
