using FoodDiary.Contracts.Hydration;
using FoodDiary.Domain.Entities.Ai;
using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.Entities.Content;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Entities.Shopping;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Hydration.Mappings;

public static class HydrationMappings
{
    public static HydrationEntryResponse ToResponse(this HydrationEntry entry) =>
        new(
            entry.Id.Value,
            entry.Timestamp,
            entry.AmountMl);
}

