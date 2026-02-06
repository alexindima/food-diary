using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Infrastructure.Persistence.Converters;

/// <summary>
/// Value Converters для строготипизированных идентификаторов
/// </summary>
public static class StronglyTypedIdConverters
{
    public static class UserIdConverter
    {
        public static ValueConverter<UserId, Guid> Instance { get; } = new(
            id => id.Value,
            value => new UserId(value));
    }

    public static class ProductIdConverter
    {
        public static ValueConverter<ProductId, Guid> Instance { get; } = new(
            id => id.Value,
            value => new ProductId(value));
    }

    public static class MealIdConverter
    {
        public static ValueConverter<MealId, Guid> Instance { get; } = new(
            id => id.Value,
            value => new MealId(value));
    }

    public static class RecipeIdConverter
    {
        public static ValueConverter<RecipeId, Guid> Instance { get; } = new(
            id => id.Value,
            value => new RecipeId(value));
    }

    public static class MealItemIdConverter
    {
        public static ValueConverter<MealItemId, Guid> Instance { get; } = new(
            id => id.Value,
            value => new MealItemId(value));
    }

    public static class MealAiSessionIdConverter
    {
        public static ValueConverter<MealAiSessionId, Guid> Instance { get; } = new(
            id => id.Value,
            value => new MealAiSessionId(value));
    }

    public static class MealAiItemIdConverter
    {
        public static ValueConverter<MealAiItemId, Guid> Instance { get; } = new(
            id => id.Value,
            value => new MealAiItemId(value));
    }

    public static class RecipeIngredientIdConverter
    {
        public static ValueConverter<RecipeIngredientId, Guid> Instance { get; } = new(
            id => id.Value,
            value => new RecipeIngredientId(value));
    }

    public static class RecipeStepIdConverter
    {
        public static ValueConverter<RecipeStepId, Guid> Instance { get; } = new(
            id => id.Value,
            value => new RecipeStepId(value));
    }
}
