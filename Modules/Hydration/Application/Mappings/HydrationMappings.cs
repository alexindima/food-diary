using FoodDiary.Modules.Hydration.Domain.Entities.Tracking;
using FoodDiary.Modules.Hydration.Application.Abstractions.Models;
using FoodDiary.Modules.Hydration.Contracts.Models;

namespace FoodDiary.Modules.Hydration.Application.Mappings;

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
