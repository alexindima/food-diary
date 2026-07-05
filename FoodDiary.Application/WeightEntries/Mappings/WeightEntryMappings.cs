using FoodDiary.Application.Abstractions.WeightEntries.Models;
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

    public static WeightEntryModel ToModel(this WeightEntryReadModel entry) =>
        new(
            entry.Id,
            entry.UserId,
            entry.Date,
            entry.Weight);
}
