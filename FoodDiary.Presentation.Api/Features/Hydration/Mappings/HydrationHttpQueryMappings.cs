using FoodDiary.Application.Hydration.Queries.GetHydrationDailyTotal;
using FoodDiary.Application.Hydration.Queries.GetHydrationEntries;
using FoodDiary.Presentation.Api.Features.Hydration.Requests;

namespace FoodDiary.Presentation.Api.Features.Hydration.Mappings;

public static class HydrationHttpQueryMappings {
    public static GetHydrationEntriesQuery ToEntriesQuery(this GetHydrationEntriesHttpQuery query, Guid userId, DateTime utcNow) {
        return new GetHydrationEntriesQuery(userId, query.DateUtc ?? utcNow);
    }

    public static GetHydrationDailyTotalQuery ToDailyQuery(this GetHydrationEntriesHttpQuery query, Guid userId, DateTime utcNow) {
        return new GetHydrationDailyTotalQuery(userId, query.DateUtc ?? utcNow);
    }
}
