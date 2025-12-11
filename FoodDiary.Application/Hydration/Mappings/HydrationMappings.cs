using FoodDiary.Contracts.Hydration;
using FoodDiary.Domain.Entities;

namespace FoodDiary.Application.Hydration.Mappings;

public static class HydrationMappings
{
    public static HydrationEntryResponse ToResponse(this HydrationEntry entry) =>
        new(
            entry.Id.Value,
            entry.Timestamp,
            entry.AmountMl);
}
