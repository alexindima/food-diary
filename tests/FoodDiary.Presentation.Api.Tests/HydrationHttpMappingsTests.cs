using FoodDiary.Application.Hydration.Commands.CreateHydrationEntry;
using FoodDiary.Application.Hydration.Commands.DeleteHydrationEntry;
using FoodDiary.Application.Hydration.Commands.UpdateHydrationEntry;
using FoodDiary.Application.Hydration.Queries.GetHydrationDailyTotal;
using FoodDiary.Application.Hydration.Queries.GetHydrationEntries;
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

        Assert.Multiple(
            () => Assert.Equal(userId, command.UserId),
            () => Assert.Equal(timestamp, command.TimestampUtc),
            () => Assert.Equal(500, command.AmountMl));
    }

    [Fact]
    public void UpdateHydrationEntryRequest_ToCommand_MapsAllFields() {
        var userId = Guid.NewGuid();
        var entryId = Guid.NewGuid();
        DateTime timestamp = DateTime.UtcNow;
        var request = new UpdateHydrationEntryHttpRequest(timestamp, 750);

        UpdateHydrationEntryCommand command = request.ToCommand(userId, entryId);

        Assert.Multiple(
            () => Assert.Equal(userId, command.UserId),
            () => Assert.Equal(entryId, command.HydrationEntryId),
            () => Assert.Equal(timestamp, command.TimestampUtc),
            () => Assert.Equal(750, command.AmountMl));
    }

    [Fact]
    public void ToDeleteCommand_MapsUserIdAndEntryId() {
        var userId = Guid.NewGuid();
        var entryId = Guid.NewGuid();

        DeleteHydrationEntryCommand command = entryId.ToDeleteCommand(userId);

        Assert.Equal(userId, command.UserId);
        Assert.Equal(entryId, command.HydrationEntryId);
    }

    [Fact]
    public void GetHydrationEntriesHttpQuery_ToEntriesQuery_UsesProvidedDate() {
        var userId = Guid.NewGuid();
        DateTime date = new(2026, 6, 14, 0, 0, 0, DateTimeKind.Utc);
        DateTime utcNow = date.AddDays(1);
        var query = new GetHydrationEntriesHttpQuery(date);

        GetHydrationEntriesQuery result = query.ToEntriesQuery(userId, utcNow);

        Assert.Equal(userId, result.UserId);
        Assert.Equal(date, result.DateUtc);
    }

    [Fact]
    public void GetHydrationEntriesHttpQuery_ToEntriesQuery_UsesCurrentUtcWhenDateMissing() {
        var userId = Guid.NewGuid();
        DateTime utcNow = new(2026, 6, 14, 8, 0, 0, DateTimeKind.Utc);
        var query = new GetHydrationEntriesHttpQuery();

        GetHydrationEntriesQuery result = query.ToEntriesQuery(userId, utcNow);

        Assert.Equal(userId, result.UserId);
        Assert.Equal(utcNow, result.DateUtc);
    }

    [Fact]
    public void GetHydrationEntriesHttpQuery_ToDailyQuery_UsesProvidedDate() {
        var userId = Guid.NewGuid();
        DateTime date = new(2026, 6, 14, 0, 0, 0, DateTimeKind.Utc);
        var query = new GetHydrationEntriesHttpQuery(date);

        GetHydrationDailyTotalQuery result = query.ToDailyQuery(userId, date.AddDays(1));

        Assert.Equal(userId, result.UserId);
        Assert.Equal(date, result.DateUtc);
    }

    [Fact]
    public void GetHydrationEntriesHttpQuery_ToDailyQuery_UsesCurrentUtcWhenDateMissing() {
        var userId = Guid.NewGuid();
        DateTime utcNow = new(2026, 6, 14, 8, 0, 0, DateTimeKind.Utc);
        var query = new GetHydrationEntriesHttpQuery();

        GetHydrationDailyTotalQuery result = query.ToDailyQuery(userId, utcNow);

        Assert.Equal(userId, result.UserId);
        Assert.Equal(utcNow, result.DateUtc);
    }
}
