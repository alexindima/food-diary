using FoodDiary.Application.Hydration.Models;
using FoodDiary.Domain.Entities.Tracking;

namespace FoodDiary.Application.Hydration.Mappings;

public static class HydrationMappings {
    public static HydrationEntryModel ToModel(this HydrationEntry entry) =>
        new(
            entry.Id.Value,
            entry.Timestamp,
            entry.AmountMl);
}
