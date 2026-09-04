using System.Globalization;
using FoodDiary.Application.Abstractions.WaistEntries.Common;
using FoodDiary.Application.Abstractions.WeightEntries.Common;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class MeasurementErrorContractTests {
    [Fact]
    public void Factories_AreOwnedByBodyMetrics() {
        Assert.Multiple(
            () => Assert.Equal("FoodDiary.Modules.BodyMetrics.Application.Abstractions", typeof(WeightEntryErrors).Assembly.GetName().Name),
            () => Assert.Equal("FoodDiary.Modules.BodyMetrics.Application.Abstractions", typeof(WaistEntryErrors).Assembly.GetName().Name),
            () => Assert.Equal("FoodDiary.Application.Abstractions.WeightEntries.Common", typeof(WeightEntryErrors).Namespace),
            () => Assert.Equal("FoodDiary.Application.Abstractions.WaistEntries.Common", typeof(WaistEntryErrors).Namespace));
    }

    [Fact]
    public void WeightErrors_PreserveNotFoundAndInaccessibleContracts() {
        var id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        AssertError(WeightEntryErrors.NotFound(id), "WeightEntry.NotFound",
            "Weight entry with ID aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee was not found.", ErrorKind.NotFound);
        AssertError(WeightEntryErrors.NotAccessible(id), "WeightEntry.NotAccessible",
            "Weight entry with ID aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee was not found or is not accessible.", ErrorKind.NotFound);
    }

    [Fact]
    public void WaistErrors_PreserveNotFoundAndInaccessibleContracts() {
        var id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        AssertError(WaistEntryErrors.NotFound(id), "WaistEntry.NotFound",
            "Waist entry with ID aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee was not found.", ErrorKind.NotFound);
        AssertError(WaistEntryErrors.NotAccessible(id), "WaistEntry.NotAccessible",
            "Waist entry with ID aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee was not found or is not accessible.", ErrorKind.NotFound);
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("ru-RU")]
    [InlineData("ar-SA")]
    public void DateConflicts_UseInvariantGregorianDate(string cultureName) {
        CultureInfo previous = CultureInfo.CurrentCulture;
        try {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
            var date = new DateTime(2024, 2, 29, 23, 59, 0, DateTimeKind.Utc);
            AssertError(WeightEntryErrors.AlreadyExists(date), "WeightEntry.AlreadyExists",
                "Weight entry for 2024-02-29 already exists.", ErrorKind.Conflict);
            AssertError(WaistEntryErrors.AlreadyExists(date), "WaistEntry.AlreadyExists",
                "Waist entry for 2024-02-29 already exists.", ErrorKind.Conflict);
        } finally {
            CultureInfo.CurrentCulture = previous;
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
