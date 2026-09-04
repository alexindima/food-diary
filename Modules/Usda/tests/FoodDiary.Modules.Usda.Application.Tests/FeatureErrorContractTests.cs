using FoodDiary.Application.Abstractions.Usda.Common;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class FeatureErrorContractTests {
    [Fact]
    public void UsdaErrors_HasOwnerAssemblyAndNamespace() {
        Assert.Multiple(
            () => Assert.Equal("FoodDiary.Modules.Usda.Application.Abstractions", typeof(UsdaErrors).Assembly.GetName().Name),
            () => Assert.Equal("FoodDiary.Application.Abstractions.Usda.Common", typeof(UsdaErrors).Namespace));
    }

    [Fact]
    public void UsdaErrors_PreservesEveryPublicErrorContract() {
        AssertError(UsdaErrors.DailyMicronutrientItemLimitExceeded(-123), "Usda.DailyMicronutrientItemLimitExceeded", "Daily micronutrient summaries support at most -123 product items.", ErrorKind.RateLimited);
        AssertError(UsdaErrors.FoodNotFound(-123), "Usda.FoodNotFound", "USDA food with FDC ID -123 was not found.", ErrorKind.NotFound);
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("ru-RU")]
    [InlineData("ar-SA")]
    public void UsdaErrors_FormatsParametersInvariantly(string cultureName) {
        System.Globalization.CultureInfo previous = System.Globalization.CultureInfo.CurrentCulture;
        try {
            var culture = (System.Globalization.CultureInfo)System.Globalization.CultureInfo.GetCultureInfo(cultureName).Clone();
            culture.NumberFormat.NegativeSign = "NEG";
            System.Globalization.CultureInfo.CurrentCulture = culture;
            AssertError(UsdaErrors.DailyMicronutrientItemLimitExceeded(-123), "Usda.DailyMicronutrientItemLimitExceeded", "Daily micronutrient summaries support at most -123 product items.", ErrorKind.RateLimited);
            AssertError(UsdaErrors.FoodNotFound(-123), "Usda.FoodNotFound", "USDA food with FDC ID -123 was not found.", ErrorKind.NotFound);
        } finally {
            System.Globalization.CultureInfo.CurrentCulture = previous;
        }
    }

    private static void AssertError(Error error, string code, string message, ErrorKind kind) {
        Assert.Multiple(
            () => Assert.Equal(code, error.Code),
            () => Assert.Equal(message, error.Message),
            () => Assert.Equal(kind, error.Kind),
            () => Assert.Null(error.Details));
    }
}
