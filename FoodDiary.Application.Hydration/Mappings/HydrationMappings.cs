using FoodDiary.Application.Abstractions.Hydration.Models;
using FoodDiary.Application.Hydration.Models;
using FoodDiary.Domain.Entities.Tracking;

namespace FoodDiary.Application.Hydration.Mappings;

public static class HydrationMappings {
    public static HydrationEntryModel ToModel(this HydrationEntry entry) =>
        new(
            entry.Id.Value,
            entry.Timestamp,
            entry.AmountMl);

    public static HydrationEntryModel ToModel(this HydrationEntryReadModel entry) =>
        new(
            entry.Id,
            entry.Timestamp,
            entry.AmountMl);
}
