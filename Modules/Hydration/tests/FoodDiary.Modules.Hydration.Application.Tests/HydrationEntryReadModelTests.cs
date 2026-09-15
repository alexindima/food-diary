using FoodDiary.Modules.Hydration.Application.Abstractions.Models;

namespace FoodDiary.Modules.Hydration.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class HydrationEntryReadModelTests {
    [Fact]
    public void Constructor_AssignsRecordProperties() {
        var id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var timestamp = new DateTime(2026, 7, 8, 12, 30, 0, DateTimeKind.Utc);

        var model = new HydrationEntryReadModel(id, timestamp, AmountMl: 350);

        Assert.Multiple(
            () => Assert.Equal(id, model.Id),
            () => Assert.Equal(timestamp, model.Timestamp),
            () => Assert.Equal(350, model.AmountMl));
    }
}
