using FoodDiary.Presentation.Api.Features.Hydration.Mappings;
using FoodDiary.Presentation.Api.Features.Hydration.Requests;

namespace FoodDiary.Presentation.Api.Tests;

public sealed class HydrationHttpMappingsTests {
    [Fact]
    public void CreateHydrationEntryRequest_ToCommand_MapsAllFields() {
        var userId = Guid.NewGuid();
        var timestamp = DateTime.UtcNow;
        var request = new CreateHydrationEntryHttpRequest(timestamp, 500);

        var command = request.ToCommand(userId);

        Assert.Equal(userId, command.UserId);
        Assert.Equal(timestamp, command.TimestampUtc);
        Assert.Equal(500, command.AmountMl);
    }

    [Fact]
    public void UpdateHydrationEntryRequest_ToCommand_MapsAllFields() {
        var userId = Guid.NewGuid();
        var entryId = Guid.NewGuid();
        var timestamp = DateTime.UtcNow;
        var request = new UpdateHydrationEntryHttpRequest(timestamp, 750);

        var command = request.ToCommand(userId, entryId);

        Assert.Equal(userId, command.UserId);
        Assert.Equal(entryId, command.HydrationEntryId);
        Assert.Equal(timestamp, command.TimestampUtc);
        Assert.Equal(750, command.AmountMl);
    }

    [Fact]
    public void ToDeleteCommand_MapsUserIdAndEntryId() {
        var userId = Guid.NewGuid();
        var entryId = Guid.NewGuid();

        var command = entryId.ToDeleteCommand(userId);

        Assert.Equal(userId, command.UserId);
        Assert.Equal(entryId, command.HydrationEntryId);
    }
}
