using FoodDiary.Application.Abstractions.WaistEntries.Models;
using FoodDiary.Application.WaistEntries.Models;
using FoodDiary.Domain.Entities.Tracking;

namespace FoodDiary.Application.WaistEntries.Mappings;

public static class WaistEntryMappings {
    public static WaistEntryModel ToModel(this WaistEntry entry) =>
        new(
            entry.Id.Value,
            entry.UserId.Value,
            entry.Date,
            entry.Circumference);

    public static WaistEntryModel ToModel(this WaistEntryReadModel entry) =>
        new(
            entry.Id,
            entry.UserId,
            entry.Date,
            entry.Circumference);
}
