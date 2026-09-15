using FoodDiary.Modules.Products.Application.Mappings;
using FoodDiary.Modules.Products.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Products.Domain.Contracts.Enums;

namespace FoodDiary.Modules.Products.Application.Tests.Nutrition;

[ExcludeFromCodeCoverage]
public sealed class NutritionMappingCompatibilityTests {
    [Theory]
    [InlineData(0, 0, 50, "yellow")]
    [InlineData(100, 0.1, 26, "red")]
    [InlineData(100, 0.3, 26, "red")]
    public void ProductsMappings_PreserveSharedQuality(double calories, double fiber, int expectedScore, string expectedGrade) {
        var userId = UserId.New();
        var product = Product.Create(userId, "Quality sample", MeasurementUnit.G, 100, 100, calories, 0, 0, 0, fiber, 0);

        FoodDiary.Modules.Products.Application.Models.ProductModel productModel = product.ToModel();

        Assert.Multiple(
            () => Assert.Equal(expectedScore, product.GetQualityScore().Score),
            () => Assert.Equal(expectedScore, productModel.QualityScore),
            () => Assert.Equal(expectedGrade, productModel.QualityGrade));
    }
}
