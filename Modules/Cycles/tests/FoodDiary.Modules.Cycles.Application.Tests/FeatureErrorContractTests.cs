using FoodDiary.Application.Abstractions.Cycles.Common;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class FeatureErrorContractTests {
    [Fact]
    public void CycleErrors_HasOwnerAssemblyAndNamespace() {
        Assert.Multiple(
            () => Assert.Equal("FoodDiary.Modules.Cycles.Application.Abstractions", typeof(CycleErrors).Assembly.GetName().Name),
            () => Assert.Equal("FoodDiary.Application.Abstractions.Cycles.Common", typeof(CycleErrors).Namespace));
    }

    [Fact]
    public void CycleErrors_PreservesEveryPublicErrorContract() {
        AssertError(CycleErrors.NotFound(Guid.Parse("12345678-1234-1234-1234-123456789abc")), "Cycle.NotFound", "Cycle with ID 12345678-1234-1234-1234-123456789abc was not found.", ErrorKind.NotFound);
    }

    [Fact]
    public void CycleDayErrors_HasOwnerAssemblyAndNamespace() {
        Assert.Multiple(
            () => Assert.Equal("FoodDiary.Modules.Cycles.Application.Abstractions", typeof(CycleDayErrors).Assembly.GetName().Name),
            () => Assert.Equal("FoodDiary.Application.Abstractions.Cycles.Common", typeof(CycleDayErrors).Namespace));
    }

    [Fact]
    public void CycleDayErrors_PreservesEveryPublicErrorContract() {
        AssertError(CycleDayErrors.NotFound(new DateOnly(2024, 2, 29)), "CycleDay.NotFound", "Cycle day for 2024-02-29 was not found.", ErrorKind.NotFound);
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("ru-RU")]
    [InlineData("ar-SA")]
    public void CycleDayErrors_FormatsParametersInvariantly(string cultureName) {
        System.Globalization.CultureInfo previous = System.Globalization.CultureInfo.CurrentCulture;
        try {
            var culture = (System.Globalization.CultureInfo)System.Globalization.CultureInfo.GetCultureInfo(cultureName).Clone();
            culture.NumberFormat.NegativeSign = "NEG";
            System.Globalization.CultureInfo.CurrentCulture = culture;
            AssertError(CycleDayErrors.NotFound(new DateOnly(2024, 2, 29)), "CycleDay.NotFound", "Cycle day for 2024-02-29 was not found.", ErrorKind.NotFound);
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
