using FoodDiary.Contracts.WaistEntries;
using FoodDiary.Domain.Entities.Ai;
using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.Entities.Content;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Entities.Shopping;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Users;

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

