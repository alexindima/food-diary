using FoodDiary.Modules.BodyMetrics.Application.Abstractions.WeightEntries.Models;
using FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Models;
using FoodDiary.Modules.BodyMetrics.Domain.Entities.Tracking;

namespace FoodDiary.Modules.BodyMetrics.Application.WeightEntries.Mappings;

public static class WeightEntryMappings {
    public static WeightEntryModel ToModel(this WeightEntry entry) =>
        new(
            entry.Id.Value,
            entry.UserId.Value,
            entry.Date,
            entry.WeightKg);

    public static WeightEntryModel ToModel(this WeightEntryReadModel entry) =>
        new(
            entry.Id,
            entry.UserId,
            entry.Date,
            entry.WeightKg);
}
