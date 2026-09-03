using System.Data;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FoodDiary.Infrastructure.Tests.Persistence;

[ExcludeFromCodeCoverage]
public sealed class StronglyTypedIdModelTests {
    private static readonly IReadOnlyDictionary<Type, Func<Guid, object>> IdFactories =
        new Dictionary<Type, Func<Guid, object>> {
            [typeof(UserId)] = value => new UserId(value),
            [typeof(FastingPlanId)] = value => new FastingPlanId(value),
            [typeof(FastingOccurrenceId)] = value => new FastingOccurrenceId(value),
            [typeof(FastingCheckInId)] = value => new FastingCheckInId(value),
            [typeof(WebPushSubscriptionId)] = value => new WebPushSubscriptionId(value),
            [typeof(ProductId)] = value => new ProductId(value),
            [typeof(MealId)] = value => new MealId(value),
            [typeof(RecipeId)] = value => new RecipeId(value),
            [typeof(MealItemId)] = value => new MealItemId(value),
            [typeof(MealAiSessionId)] = value => new MealAiSessionId(value),
            [typeof(MealAiItemId)] = value => new MealAiItemId(value),
            [typeof(RecipeIngredientId)] = value => new RecipeIngredientId(value),
            [typeof(RecipeStepId)] = value => new RecipeStepId(value),
        };

    [Theory]
    [InlineData(typeof(UserId))]
    [InlineData(typeof(FastingPlanId))]
    [InlineData(typeof(FastingOccurrenceId))]
    [InlineData(typeof(FastingCheckInId))]
    [InlineData(typeof(WebPushSubscriptionId))]
    [InlineData(typeof(ProductId))]
    [InlineData(typeof(MealId))]
    [InlineData(typeof(RecipeId))]
    [InlineData(typeof(MealItemId))]
    [InlineData(typeof(MealAiSessionId))]
    [InlineData(typeof(MealAiItemId))]
    [InlineData(typeof(RecipeIngredientId))]
    [InlineData(typeof(RecipeStepId))]
    public void ComposedModel_ConvertsEveryMappedIdPropertyToUuid(Type idType) {
        using var context = new FoodDiaryDbContext(new DbContextOptionsBuilder<FoodDiaryDbContext>()
            .UseNpgsql("Host=localhost;Database=id_converter_model;Username=unused")
            .Options);
        IProperty[] properties = [.. context.Model.GetEntityTypes()
            .SelectMany(entity => entity.GetProperties())
            .Where(property => (Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType) == idType)];

        Assert.NotEmpty(properties);
        Assert.Contains(properties, property => property.IsPrimaryKey());
        foreach (IProperty property in properties) {
            RelationalTypeMapping mapping = property.GetRelationalTypeMapping();
            ValueConverter converter = Assert.IsAssignableFrom<ValueConverter>(mapping.Converter);
            Assert.Multiple(
                () => Assert.Equal("uuid", mapping.StoreType),
                () => Assert.Equal(typeof(Guid), Nullable.GetUnderlyingType(converter.ProviderClrType) ?? converter.ProviderClrType),
                () => Assert.Equal(idType, Nullable.GetUnderlyingType(converter.ModelClrType) ?? converter.ModelClrType));

            foreach (Guid value in new[] { Guid.Empty, Guid.Parse("95da1051-b637-4c5e-9bae-1cec40703732") }) {
                object id = IdFactories[idType](value);
                Assert.Multiple(
                    () => Assert.Equal(value, converter.ConvertToProvider(id)),
                    () => Assert.Equal(id, converter.ConvertFromProvider(value)));
            }

            if (property.IsNullable) {
                Assert.Multiple(
                    () => Assert.Null(converter.ConvertToProvider(null)),
                    () => Assert.Null(converter.ConvertFromProvider(null)));
            }
        }

        Assert.Equal(ConnectionState.Closed, context.Database.GetDbConnection().State);
    }
}
