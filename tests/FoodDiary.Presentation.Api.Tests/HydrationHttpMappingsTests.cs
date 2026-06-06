using FoodDiary.Application.Hydration.Commands.CreateHydrationEntry;
using FoodDiary.Application.Hydration.Commands.DeleteHydrationEntry;
using FoodDiary.Application.Hydration.Commands.UpdateHydrationEntry;
using FoodDiary.Presentation.Api.Features.Hydration.Mappings;
using FoodDiary.Presentation.Api.Features.Hydration.Requests;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class HydrationHttpMappingsTests {
    [Fact]
    public void CreateHydrationEntryRequest_ToCommand_MapsAllFields() {
        var userId = Guid.NewGuid();
        DateTime timestamp = DateTime.UtcNow;
        var request = new CreateHydrationEntryHttpRequest(timestamp, 500);

        CreateHydrationEntryCommand command = request.ToCommand(userId);

        Assert.Equal(userId, command.UserId);
        Assert.Equal(timestamp, command.TimestampUtc);
        Assert.Equal(500, command.AmountMl);
    }

    [Fact]
    public void UpdateHydrationEntryRequest_ToCommand_MapsAllFields() {
        var userId = Guid.NewGuid();
        var entryId = Guid.NewGuid();
        DateTime timestamp = DateTime.UtcNow;
        var request = new UpdateHydrationEntryHttpRequest(timestamp, 750);

        UpdateHydrationEntryCommand command = request.ToCommand(userId, entryId);

        Assert.Equal(userId, command.UserId);
        Assert.Equal(entryId, command.HydrationEntryId);
        Assert.Equal(timestamp, command.TimestampUtc);
        Assert.Equal(750, command.AmountMl);
    }

    [Fact]
    public void ToDeleteCommand_MapsUserIdAndEntryId() {
        var userId = Guid.NewGuid();
        var entryId = Guid.NewGuid();

        DeleteHydrationEntryCommand command = entryId.ToDeleteCommand(userId);

        Assert.Equal(userId, command.UserId);
        Assert.Equal(entryId, command.HydrationEntryId);
    }
}
