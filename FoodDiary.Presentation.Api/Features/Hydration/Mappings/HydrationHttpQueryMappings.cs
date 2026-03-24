using FoodDiary.Application.Hydration.Queries.GetHydrationDailyTotal;
using FoodDiary.Application.Hydration.Queries.GetHydrationEntries;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Presentation.Api.Features.Hydration.Requests;

namespace FoodDiary.Presentation.Api.Features.Hydration.Mappings;

public static class HydrationHttpQueryMappings {
    public static GetHydrationEntriesQuery ToEntriesQuery(this GetHydrationEntriesHttpQuery query, UserId userId) {
        return new GetHydrationEntriesQuery(userId, query.DateUtc ?? DateTime.UtcNow);
    }

    public static GetHydrationDailyTotalQuery ToDailyQuery(this GetHydrationEntriesHttpQuery query, UserId userId) {
        return new GetHydrationDailyTotalQuery(userId, query.DateUtc ?? DateTime.UtcNow);
    }
}
