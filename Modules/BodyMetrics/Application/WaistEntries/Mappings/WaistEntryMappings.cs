using FoodDiary.Modules.BodyMetrics.Application.Abstractions.WaistEntries.Models;
using FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Models;
using FoodDiary.Modules.BodyMetrics.Domain.Entities.Tracking;

namespace FoodDiary.Modules.BodyMetrics.Application.WaistEntries.Mappings;

public static class WaistEntryMappings {
    public static WaistEntryModel ToModel(this WaistEntry entry) =>
        new(
            entry.Id.Value,
            entry.UserId.Value,
            entry.Date,
            entry.CircumferenceCm);

    public static WaistEntryModel ToModel(this WaistEntryReadModel entry) =>
        new(
            entry.Id,
            entry.UserId,
            entry.Date,
            entry.CircumferenceCm);
}
