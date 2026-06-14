using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence.Converters;

namespace FoodDiary.Infrastructure.Tests.Persistence;

[ExcludeFromCodeCoverage]
public sealed class StronglyTypedIdConvertersTests {
    [Fact]
    public void ConverterInstances_RoundTripStronglyTypedIds() {
        var value = Guid.NewGuid();

        Assert.Equal(value, StronglyTypedIdConverters.UserIdConverter.Instance.ConvertToProvider(new UserId(value)));
        Assert.Equal(new UserId(value), StronglyTypedIdConverters.UserIdConverter.Instance.ConvertFromProvider(value));
        Assert.Equal(value, StronglyTypedIdConverters.FastingPlanIdConverter.Instance.ConvertToProvider(new FastingPlanId(value)));
        Assert.Equal(new FastingPlanId(value), StronglyTypedIdConverters.FastingPlanIdConverter.Instance.ConvertFromProvider(value));
        Assert.Equal(value, StronglyTypedIdConverters.FastingOccurrenceIdConverter.Instance.ConvertToProvider(new FastingOccurrenceId(value)));
        Assert.Equal(new FastingOccurrenceId(value), StronglyTypedIdConverters.FastingOccurrenceIdConverter.Instance.ConvertFromProvider(value));
        Assert.Equal(value, StronglyTypedIdConverters.FastingCheckInIdConverter.Instance.ConvertToProvider(new FastingCheckInId(value)));
        Assert.Equal(new FastingCheckInId(value), StronglyTypedIdConverters.FastingCheckInIdConverter.Instance.ConvertFromProvider(value));
        Assert.Equal(value, StronglyTypedIdConverters.WebPushSubscriptionIdConverter.Instance.ConvertToProvider(new WebPushSubscriptionId(value)));
        Assert.Equal(new WebPushSubscriptionId(value), StronglyTypedIdConverters.WebPushSubscriptionIdConverter.Instance.ConvertFromProvider(value));
        Assert.Equal(value, StronglyTypedIdConverters.ProductIdConverter.Instance.ConvertToProvider(new ProductId(value)));
        Assert.Equal(new ProductId(value), StronglyTypedIdConverters.ProductIdConverter.Instance.ConvertFromProvider(value));
        Assert.Equal(value, StronglyTypedIdConverters.MealIdConverter.Instance.ConvertToProvider(new MealId(value)));
        Assert.Equal(new MealId(value), StronglyTypedIdConverters.MealIdConverter.Instance.ConvertFromProvider(value));
        Assert.Equal(value, StronglyTypedIdConverters.RecipeIdConverter.Instance.ConvertToProvider(new RecipeId(value)));
        Assert.Equal(new RecipeId(value), StronglyTypedIdConverters.RecipeIdConverter.Instance.ConvertFromProvider(value));
        Assert.Equal(value, StronglyTypedIdConverters.MealItemIdConverter.Instance.ConvertToProvider(new MealItemId(value)));
        Assert.Equal(new MealItemId(value), StronglyTypedIdConverters.MealItemIdConverter.Instance.ConvertFromProvider(value));
        Assert.Equal(value, StronglyTypedIdConverters.MealAiSessionIdConverter.Instance.ConvertToProvider(new MealAiSessionId(value)));
        Assert.Equal(new MealAiSessionId(value), StronglyTypedIdConverters.MealAiSessionIdConverter.Instance.ConvertFromProvider(value));
        Assert.Equal(value, StronglyTypedIdConverters.MealAiItemIdConverter.Instance.ConvertToProvider(new MealAiItemId(value)));
        Assert.Equal(new MealAiItemId(value), StronglyTypedIdConverters.MealAiItemIdConverter.Instance.ConvertFromProvider(value));
        Assert.Equal(value, StronglyTypedIdConverters.RecipeIngredientIdConverter.Instance.ConvertToProvider(new RecipeIngredientId(value)));
        Assert.Equal(new RecipeIngredientId(value), StronglyTypedIdConverters.RecipeIngredientIdConverter.Instance.ConvertFromProvider(value));
        Assert.Equal(value, StronglyTypedIdConverters.RecipeStepIdConverter.Instance.ConvertToProvider(new RecipeStepId(value)));
        Assert.Equal(new RecipeStepId(value), StronglyTypedIdConverters.RecipeStepIdConverter.Instance.ConvertFromProvider(value));
    }
}
