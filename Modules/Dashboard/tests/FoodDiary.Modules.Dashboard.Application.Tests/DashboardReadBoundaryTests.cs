using FoodDiary.Mediator;
using FoodDiary.Modules.Dashboard.Application.Abstractions.Common;
using FoodDiary.Modules.Dashboard.Application.Abstractions.Models;
using FoodDiary.Modules.Dashboard.Application.Common;
using FoodDiary.Modules.Dashboard.Application.Models;
using FoodDiary.Modules.Dashboard.Application.Services;
using FoodDiary.Modules.Dashboard.Contracts.Models;
using FoodDiary.Modules.Hydration.Contracts.Models;
using FoodDiary.Modules.Hydration.Contracts.Queries.ReadRecentHydrationEntries;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Dashboard.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class DashboardReadBoundaryTests {
    [Theory]
    [InlineData(-1)]
    [InlineData(400)]
    public async Task InvalidUserCalendarRange_DoesNotReadProfilesOrSections(int days) {
        IDashboardUserContextService users = Substitute.For<IDashboardUserContextService>();
        ISender sender = Substitute.For<ISender>();
        IDashboardReadService reads = Substitute.For<IDashboardReadService>();
        var loader = new DashboardSectionDataLoader(sender, users, reads);
        DateTime from = new(2026, 11, 2);

        Result<DashboardBuildContext> result = await loader.CreateBuildContextAsync(
            new DashboardSnapshotRequest(Guid.NewGuid(), from, from.AddDays(days), "en", 7, 1, 10, UseUserTimeZone: true), CancellationToken.None);

        ResultAssert.Failure(result, "Validation.Invalid");
        Assert.Empty(users.ReceivedCalls());
        Assert.Empty(sender.ReceivedCalls());
        Assert.Empty(reads.ReceivedCalls());
    }

    [Fact]
    public async Task UserTimeZoneProfileFailure_DoesNotReadAnyDashboardSections() {
        var owner = UserId.New();
        IDashboardUserContextService users = Substitute.For<IDashboardUserContextService>();
        var failure = new Error("Profile.Unavailable", "Profile unavailable.");
        users.GetAccessibleDashboardUserAsync(owner, Arg.Any<CancellationToken>(), Arg.Any<DateOnly?>(), Arg.Any<DateOnly?>())
            .Returns(Result.Failure<DashboardUserContextModel>(failure));
        ISender sender = Substitute.For<ISender>();
        IDashboardReadService reads = Substitute.For<IDashboardReadService>();
        var loader = new DashboardSectionDataLoader(sender, users, reads);
        var request = new DashboardSnapshotRequest(owner.Value, new DateTime(2026, 11, 2), DateTo: null, "en", 7, 1, 10, UseUserTimeZone: true);

        Result<DashboardBuildContext> result = await loader.CreateBuildContextAsync(request, CancellationToken.None);

        Assert.Equal(failure, ResultAssert.Failure(result));
        Assert.Empty(sender.ReceivedCalls());
        Assert.Empty(reads.ReceivedCalls());
    }

    [Fact]
    public async Task HydrationHistory_MapsVisibleEntriesAndDetectsTheExtraPaginationRow() {
        var owner = UserId.New();
        DateTime start = new(2026, 11, 2, 0, 0, 0, DateTimeKind.Utc);
        DateTime end = start.AddDays(1).AddTicks(-1);
        HydrationEntryModel[] entries = [.. Enumerable.Range(0, DashboardHydrationHistoryModel.EntryLimit + 1)
            .Select(index => new HydrationEntryModel(Guid.NewGuid(), start.AddMinutes(index), 100 + index))];
        ISender sender = Substitute.For<ISender>();
        sender.Send(new ReadRecentHydrationEntriesQuery(owner, start, end, entries.Length), Arg.Any<CancellationToken>())
            .Returns(entries);
        var reader = new RepositoryDashboardBodyReadService(sender);

        DashboardBodyReadModel result = await reader.GetBodyAsync(owner, start, end, start, 1,
            includeWeight: false, includeWaist: false, includeHydration: true, CancellationToken.None);

        Assert.NotNull(result.HydrationHistory);
        Assert.True(result.HydrationHistory.HasMore);
        Assert.Equal(DashboardHydrationHistoryModel.EntryLimit, result.HydrationHistory.Entries.Count);
        DashboardHydrationEntryModel first = result.HydrationHistory.Entries[0];
        Assert.Multiple(
            () => Assert.Equal(entries[0].Id, first.Id),
            () => Assert.Equal(entries[0].TimestampUtc, first.TimestampUtc),
            () => Assert.Equal(entries[0].AmountMl, first.AmountMl));
    }
}
