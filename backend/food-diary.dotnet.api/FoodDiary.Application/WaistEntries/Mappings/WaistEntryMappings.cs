using FoodDiary.Contracts.WaistEntries;
using FoodDiary.Domain.Entities;

namespace FoodDiary.Application.WaistEntries.Mappings;

public static class WaistEntryMappings
{
    public static WaistEntryResponse ToResponse(this WaistEntry entry) =>
        new(
            entry.Id.Value,
            entry.UserId.Value,
            entry.Date,
            entry.Circumference);
}
