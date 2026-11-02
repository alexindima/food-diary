using FoodDiary.Modules.Hydration.Application.Abstractions.Common;
using FoodDiary.Modules.Hydration.Application.Abstractions.Models;
using FoodDiary.Modules.Hydration.Application.Queries.ReadRecentHydrationEntries;
using FoodDiary.Modules.Hydration.Contracts.Models;
using FoodDiary.Modules.Hydration.Contracts.Queries.ReadRecentHydrationEntries;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Hydration.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class ReadRecentHydrationEntriesTests {
    [Theory]
    [InlineData(0, 1)]
    [InlineData(500, 100)]
    public async Task RecentEntries_ClampsLimitAndPreservesEntryIdentityAndTimestamp(int limit, int expectedLimit) {
        var owner = UserId.New();
        DateTime from = new(2026, 11, 2, 0, 0, 0, DateTimeKind.Utc);
        DateTime to = from.AddDays(1);
        var entry = new HydrationEntryReadModel(Guid.NewGuid(), from.AddHours(1), 350);
        IHydrationEntryReadModelRepository repository = Substitute.For<IHydrationEntryReadModelRepository>();
        using var cancellation = new CancellationTokenSource();
        repository.GetRecentReadModelsAsync(owner, from, to, expectedLimit, cancellation.Token).Returns([entry]);

        IReadOnlyList<HydrationEntryModel> result = await new ReadRecentHydrationEntriesQueryHandler(repository)
            .Handle(new ReadRecentHydrationEntriesQuery(owner, from, to, limit), cancellation.Token);

        HydrationEntryModel mapped = Assert.Single(result);
        Assert.Multiple(
            () => Assert.Equal(entry.Id, mapped.Id),
            () => Assert.Equal(entry.Timestamp, mapped.TimestampUtc),
            () => Assert.Equal(entry.AmountMl, mapped.AmountMl));
        await repository.Received(1).GetRecentReadModelsAsync(owner, from, to, expectedLimit, cancellation.Token);
    }
}
