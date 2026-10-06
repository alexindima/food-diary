using System.Reflection;
using FoodDiary.Modules.Usda.Presentation.Controllers;
using Microsoft.AspNetCore.RateLimiting;

namespace FoodDiary.Modules.Usda.Presentation.Tests;

[ExcludeFromCodeCoverage]
public sealed class UsdaProviderPolicyTests {
    [Theory]
    [InlineData(nameof(UsdaController.Search))]
    [InlineData(nameof(UsdaController.LinkProduct))]
    [InlineData(nameof(UsdaController.GetDailyMicronutrients))]
    public void ProviderBackedEndpoints_ShareFoodDetailAdmissionPolicy(string actionName) {
        MethodInfo foodDetail = typeof(UsdaController).GetMethod(nameof(UsdaController.GetDetail))!;
        MethodInfo action = typeof(UsdaController).GetMethod(actionName)!;
        EnableRateLimitingAttribute expected = Assert.IsType<EnableRateLimitingAttribute>(
            foodDetail.GetCustomAttribute<EnableRateLimitingAttribute>(inherit: true));
        EnableRateLimitingAttribute actual = Assert.IsType<EnableRateLimitingAttribute>(
            action.GetCustomAttribute<EnableRateLimitingAttribute>(inherit: true)
            ?? typeof(UsdaController).GetCustomAttribute<EnableRateLimitingAttribute>(inherit: true));

        Assert.Multiple(
            () => Assert.Equal(expected.PolicyName, actual.PolicyName),
            () => Assert.Null(action.GetCustomAttribute<DisableRateLimitingAttribute>(inherit: true)));
    }
}
