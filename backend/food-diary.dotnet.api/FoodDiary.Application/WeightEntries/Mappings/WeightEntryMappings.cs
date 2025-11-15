using FoodDiary.Contracts.WeightEntries;
using FoodDiary.Domain.Entities;

namespace FoodDiary.Application.WeightEntries.Mappings;

public static class WeightEntryMappings
{
    public static WeightEntryResponse ToResponse(this WeightEntry entry) =>
        new(
            entry.Id.Value,
            entry.UserId.Value,
            entry.Date,
            entry.Weight);
}
