using FoodDiary.Application.Abstractions.Hydration.Common;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class HydrationErrorContractTests {
    [Fact]
    public void Factory_IsOwnedByModule() {
        Assert.Equal("FoodDiary.Modules.Hydration.Application.Abstractions", typeof(HydrationEntryErrors).Assembly.GetName().Name);
        Assert.Equal("FoodDiary.Application.Abstractions.Hydration.Common", typeof(HydrationEntryErrors).Namespace);
    }

    [Fact]
    public void Errors_PreservePrivacyAndRoundTripTimestamp() {
        var id = Guid.Parse("12345678-1234-1234-1234-123456789abc");
        var timestamp = new DateTime(2026, 9, 4, 1, 2, 3, DateTimeKind.Utc);
        AssertError(HydrationEntryErrors.NotFound(id), "HydrationEntry.NotFound",
            "Hydration entry with id '12345678-1234-1234-1234-123456789abc' not found", ErrorKind.NotFound);
        AssertError(HydrationEntryErrors.NotAccessible(id), "HydrationEntry.NotAccessible",
            "Hydration entry with id '12345678-1234-1234-1234-123456789abc' was not found or is not accessible.", ErrorKind.NotFound);
        AssertError(HydrationEntryErrors.AlreadyExists(timestamp), "HydrationEntry.AlreadyExists",
            "A hydration entry already exists at '2026-09-04T01:02:03.0000000Z'.", ErrorKind.Conflict);
    }

    private static void AssertError(Error error, string code, string message, ErrorKind kind) {
        Assert.Multiple(
            () => Assert.Equal(code, error.Code),
            () => Assert.Equal(message, error.Message),
            () => Assert.Equal(kind, error.Kind),
            () => Assert.Null(error.Details));
    }
}
