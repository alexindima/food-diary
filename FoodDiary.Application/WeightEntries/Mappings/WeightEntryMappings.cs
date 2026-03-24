using FoodDiary.Application.WeightEntries.Models;
using FoodDiary.Domain.Entities.Tracking;

namespace FoodDiary.Application.WeightEntries.Mappings;

public static class WeightEntryMappings {
    public static WeightEntryModel ToModel(this WeightEntry entry) =>
        new(
            entry.Id.Value,
            entry.UserId.Value,
            entry.Date,
            entry.Weight);
}
