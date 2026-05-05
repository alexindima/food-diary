using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Authentication.Models;
using FoodDiary.Application.Admin.Queries.GetAdminUserLoginEvents;
using FoodDiary.Application.Admin.Queries.GetAdminUserLoginSummary;
using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Tests.Admin;

public sealed class UserLoginActivityFeatureTests {
    [Fact]
    public async Task GetAdminUserLoginEvents_MasksIpAddressAndNormalizesPaging() {
        var repository = new RecordingUserLoginEventRepository {
            PagedItems = [
                new UserLoginEventReadModel(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "user@example.com",
                    "password",
                    "203.0.113.42",
                    "Mozilla/5.0 Chrome/125.0.0.0",
                    "Chrome",
                    "125.0.0.0",
                    "Windows",
                    "Desktop",
                    new DateTime(2030, 3, 28, 12, 0, 0, DateTimeKind.Utc))
            ],
            TotalItems = 42
        };
        var handler = new GetAdminUserLoginEventsQueryHandler(repository);

        var result = await handler.Handle(
            new GetAdminUserLoginEventsQuery(Page: 0, Limit: 500, UserId: null, Search: "chrome"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, repository.LastPage);
        Assert.Equal(20, repository.LastLimit);
        Assert.Equal("chrome", repository.LastSearch);
        Assert.Equal(3, result.Value.TotalPages);
        Assert.Equal(42, result.Value.TotalItems);
        Assert.Equal("203.0.113.0", Assert.Single(result.Value.Data).MaskedIpAddress);
    }

    [Fact]
    public async Task GetAdminUserLoginEvents_MasksIpv6Address() {
        var repository = new RecordingUserLoginEventRepository {
            PagedItems = [
                new UserLoginEventReadModel(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "user@example.com",
                    "password",
                    "2001:0db8:85a3:0000:0000:8a2e:0370:7334",
                    null,
                    null,
                    null,
                    null,
                    null,
                    new DateTime(2030, 3, 28, 12, 0, 0, DateTimeKind.Utc))
            ],
            TotalItems = 1
        };
        var handler = new GetAdminUserLoginEventsQueryHandler(repository);

        var result = await handler.Handle(
            new GetAdminUserLoginEventsQuery(Page: 1, Limit: 20, UserId: null, Search: null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("2001:0db8:85a3:0000:0000", Assert.Single(result.Value.Data).MaskedIpAddress);
    }

    [Fact]
    public async Task GetAdminUserLoginSummary_ReturnsRepositorySummary() {
        var fromUtc = new DateTime(2030, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var toUtc = new DateTime(2030, 3, 31, 23, 59, 59, DateTimeKind.Utc);
        var lastSeenAtUtc = new DateTime(2030, 3, 28, 12, 0, 0, DateTimeKind.Utc);
        var repository = new RecordingUserLoginEventRepository {
            SummaryItems = [
                new UserLoginDeviceSummaryModel("device:Desktop", 7, lastSeenAtUtc)
            ]
        };
        var handler = new GetAdminUserLoginSummaryQueryHandler(repository);

        var result = await handler.Handle(new GetAdminUserLoginSummaryQuery(fromUtc, toUtc), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(fromUtc, repository.LastSummaryFromUtc);
        Assert.Equal(toUtc, repository.LastSummaryToUtc);
        var item = Assert.Single(result.Value);
        Assert.Equal("device:Desktop", item.Key);
        Assert.Equal(7, item.Count);
        Assert.Equal(lastSeenAtUtc, item.LastSeenAtUtc);
    }

    private sealed class RecordingUserLoginEventRepository : IUserLoginEventRepository {
        public IReadOnlyList<UserLoginEventReadModel> PagedItems { get; init; } = [];
        public int TotalItems { get; init; }
        public IReadOnlyList<UserLoginDeviceSummaryModel> SummaryItems { get; init; } = [];
        public int LastPage { get; private set; }
        public int LastLimit { get; private set; }
        public string? LastSearch { get; private set; }
        public DateTime? LastSummaryFromUtc { get; private set; }
        public DateTime? LastSummaryToUtc { get; private set; }

        public Task AddAsync(UserLoginEvent loginEvent, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<(IReadOnlyList<UserLoginEventReadModel> Items, int TotalItems)> GetPagedAsync(
            int page,
            int limit,
            Guid? userId,
            string? search,
            CancellationToken cancellationToken = default) {
            LastPage = page;
            LastLimit = limit;
            LastSearch = search;
            return Task.FromResult((PagedItems, TotalItems));
        }

        public Task<IReadOnlyList<UserLoginDeviceSummaryModel>> GetDeviceSummaryAsync(
            DateTime? fromUtc,
            DateTime? toUtc,
            CancellationToken cancellationToken = default) {
            LastSummaryFromUtc = fromUtc;
            LastSummaryToUtc = toUtc;
            return Task.FromResult(SummaryItems);
        }

        public Task<int> DeleteOlderThanAsync(
            DateTime olderThanUtc,
            int batchSize,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
