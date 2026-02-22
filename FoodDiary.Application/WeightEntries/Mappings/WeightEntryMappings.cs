using FoodDiary.Contracts.WeightEntries;
using FoodDiary.Domain.Entities.Ai;
using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.Entities.Content;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Entities.Shopping;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Users;

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

