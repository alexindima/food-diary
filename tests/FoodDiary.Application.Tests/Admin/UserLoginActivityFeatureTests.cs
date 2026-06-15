using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Authentication.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Admin.Queries.GetAdminUserLoginEvents;
using FoodDiary.Application.Admin.Queries.GetAdminUserLoginSummary;
using FoodDiary.Application.Common.Models;

namespace FoodDiary.Application.Tests.Admin;

[ExcludeFromCodeCoverage]
public sealed class UserLoginActivityFeatureTests {
    [Fact]
    public async Task GetAdminUserLoginEvents_MasksIpAddressAndNormalizesPaging() {
        IReadOnlyList<UserLoginEventReadModel> items = [
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
                new DateTime(2030, 3, 28, 12, 0, 0, DateTimeKind.Utc)),
        ];
        IUserLoginEventRepository repository = CreatePagedRepository(items, 42, out Func<(int Page, int Limit, string? Search)> getLastPaged);
        GetAdminUserLoginEventsQueryHandler handler = new(repository);

        Result<PagedResponse<AdminUserLoginEventModel>> result = await handler.Handle(
            new GetAdminUserLoginEventsQuery(Page: 0, Limit: 500, UserId: null, Search: "chrome"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, getLastPaged().Page);
        Assert.Equal(20, getLastPaged().Limit);
        Assert.Equal("chrome", getLastPaged().Search);
        Assert.Equal(3, result.Value.TotalPages);
        Assert.Equal(42, result.Value.TotalItems);
        Assert.Equal("203.0.113.0", Assert.Single(result.Value.Data).MaskedIpAddress);
    }

    [Fact]
    public async Task GetAdminUserLoginEvents_MasksIpv6Address() {
        IReadOnlyList<UserLoginEventReadModel> items = [
            new UserLoginEventReadModel(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "user@example.com",
                "password",
                "2001:0db8:85a3:0000:0000:8a2e:0370:7334",
                UserAgent: null,
                BrowserName: null,
                BrowserVersion: null,
                OperatingSystem: null,
                DeviceType: null,
                new DateTime(2030, 3, 28, 12, 0, 0, DateTimeKind.Utc)),
        ];
        IUserLoginEventRepository repository = CreatePagedRepository(items, 1, out _);
        GetAdminUserLoginEventsQueryHandler handler = new(repository);

        Result<PagedResponse<AdminUserLoginEventModel>> result = await handler.Handle(
            new GetAdminUserLoginEventsQuery(Page: 1, Limit: 20, UserId: null, Search: null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("2001:0db8:85a3:0000:0000", Assert.Single(result.Value.Data).MaskedIpAddress);
    }

    [Fact]
    public async Task GetAdminUserLoginEvents_WithBlankIpAddress_ReturnsNullMaskedIpAddress() {
        IReadOnlyList<UserLoginEventReadModel> items = [
            new UserLoginEventReadModel(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "user@example.com",
                "password",
                "   ",
                UserAgent: null,
                BrowserName: null,
                BrowserVersion: null,
                OperatingSystem: null,
                DeviceType: null,
                new DateTime(2030, 3, 28, 12, 0, 0, DateTimeKind.Utc)),
        ];
        IUserLoginEventRepository repository = CreatePagedRepository(items, 1, out _);
        GetAdminUserLoginEventsQueryHandler handler = new(repository);

        Result<PagedResponse<AdminUserLoginEventModel>> result = await handler.Handle(
            new GetAdminUserLoginEventsQuery(Page: 1, Limit: 20, UserId: null, Search: null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(Assert.Single(result.Value.Data).MaskedIpAddress);
    }

    [Fact]
    public async Task GetAdminUserLoginSummary_ReturnsRepositorySummary() {
        var fromUtc = new DateTime(2030, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var toUtc = new DateTime(2030, 3, 31, 23, 59, 59, DateTimeKind.Utc);
        var lastSeenAtUtc = new DateTime(2030, 3, 28, 12, 0, 0, DateTimeKind.Utc);
        IReadOnlyList<UserLoginDeviceSummaryModel> summaryItems = [
            new UserLoginDeviceSummaryModel("device:Desktop", 7, lastSeenAtUtc),
        ];
        IUserLoginEventRepository repository = CreateSummaryRepository(summaryItems, out Func<(DateTime? FromUtc, DateTime? ToUtc)> getLastSummary);
        GetAdminUserLoginSummaryQueryHandler handler = new(repository);

        Result<IReadOnlyList<AdminUserLoginDeviceSummaryModel>> result = await handler.Handle(new GetAdminUserLoginSummaryQuery(fromUtc, toUtc), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(fromUtc, getLastSummary().FromUtc);
        Assert.Equal(toUtc, getLastSummary().ToUtc);
        AdminUserLoginDeviceSummaryModel item = Assert.Single(result.Value);
        Assert.Equal("device:Desktop", item.Key);
        Assert.Equal(7, item.Count);
        Assert.Equal(lastSeenAtUtc, item.LastSeenAtUtc);
    }

    private static IUserLoginEventRepository CreatePagedRepository(
        IReadOnlyList<UserLoginEventReadModel> items,
        int totalItems,
        out Func<(int Page, int Limit, string? Search)> getLastPaged) {
        IUserLoginEventRepository repository = Substitute.For<IUserLoginEventRepository>();
        (int Page, int Limit, string? Search) lastPaged = (0, 0, null);
        repository
            .GetPagedAsync(
                Arg.Do<int>(page => lastPaged.Page = page),
                Arg.Do<int>(limit => lastPaged.Limit = limit),
                Arg.Any<Guid?>(),
                Arg.Do<string?>(search => lastPaged.Search = search),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult((items, totalItems)));
        getLastPaged = () => lastPaged;
        return repository;
    }

    private static IUserLoginEventRepository CreateSummaryRepository(
        IReadOnlyList<UserLoginDeviceSummaryModel> items,
        out Func<(DateTime? FromUtc, DateTime? ToUtc)> getLastSummary) {
        IUserLoginEventRepository repository = Substitute.For<IUserLoginEventRepository>();
        (DateTime? FromUtc, DateTime? ToUtc) lastSummary = (null, null);
        repository
            .GetDeviceSummaryAsync(
                Arg.Do<DateTime?>(fromUtc => lastSummary.FromUtc = fromUtc),
                Arg.Do<DateTime?>(toUtc => lastSummary.ToUtc = toUtc),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(items));
        getLastSummary = () => lastSummary;
        return repository;
    }
}
