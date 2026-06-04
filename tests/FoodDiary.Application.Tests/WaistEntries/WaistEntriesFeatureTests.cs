using FoodDiary.Application.WaistEntries.Commands.CreateWaistEntry;
using FoodDiary.Application.WaistEntries.Commands.DeleteWaistEntry;
using FoodDiary.Application.WaistEntries.Commands.UpdateWaistEntry;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.WaistEntries.Common;
using FoodDiary.Application.WaistEntries.Queries.GetWaistEntries;
using FoodDiary.Application.WaistEntries.Queries.GetWaistSummaries;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.WaistEntries;

[ExcludeFromCodeCoverage]
public class WaistEntriesFeatureTests {
    [Fact]
    public async Task CreateWaistEntryCommandValidator_WithEmptyUserId_Fails() {
        var validator = new CreateWaistEntryCommandValidator();
        var command = new CreateWaistEntryCommand(Guid.Empty, DateTime.UtcNow, 80);

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task GetWaistEntriesQueryValidator_WithInvalidDateRange_Fails() {
        var validator = new GetWaistEntriesQueryValidator();
        var query = new GetWaistEntriesQuery(Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow.AddDays(-1), 10, true);

        var result = await validator.ValidateAsync(query);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task GetWaistSummariesQueryValidator_WithNonPositiveQuantization_Fails() {
        var validator = new GetWaistSummariesQueryValidator();
        var query = new GetWaistSummariesQuery(Guid.NewGuid(), DateTime.UtcNow.AddDays(-10), DateTime.UtcNow, 0);

        var result = await validator.ValidateAsync(query);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task CreateWaistEntryCommandHandler_WithEmptyUserId_ReturnsInvalidToken() {
        var repository = new InMemoryWaistEntryRepository();
        var handler = new CreateWaistEntryCommandHandler(repository, new StubUserRepository(User.Create("user@example.com", "hash")));

        var result = await handler.Handle(
            new CreateWaistEntryCommand(Guid.Empty, DateTime.UtcNow, 82),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task CreateWaistEntryCommandHandler_NormalizesDateToUtcForDuplicateLookup() {
        var repository = new InMemoryWaistEntryRepository();
        var user = User.Create("waist@example.com", "hash");
        var handler = new CreateWaistEntryCommandHandler(repository, new StubUserRepository(user));
        var userId = user.Id;
        var localDate = new DateTime(2026, 2, 23, 23, 30, 0, DateTimeKind.Local);
        var expectedDate = NormalizeUtcDate(localDate);

        var result = await handler.Handle(
            new CreateWaistEntryCommand(userId.Value, localDate, 82),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(expectedDate, repository.LastGetByDateDate);
        Assert.Equal(DateTimeKind.Utc, repository.LastGetByDateDate.Kind);
    }

    [Fact]
    public async Task CreateWaistEntryCommandHandler_WithDateOnlyValue_PreservesRequestedCalendarDate() {
        var repository = new InMemoryWaistEntryRepository();
        var user = User.Create("waist-dateonly@example.com", "hash");
        var handler = new CreateWaistEntryCommandHandler(repository, new StubUserRepository(user));
        var dateOnly = new DateTime(2026, 5, 27, 0, 0, 0, DateTimeKind.Unspecified);
        var expectedDate = new DateTime(2026, 5, 27, 0, 0, 0, DateTimeKind.Utc);

        var result = await handler.Handle(
            new CreateWaistEntryCommand(user.Id.Value, dateOnly, 82),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(expectedDate, repository.LastGetByDateDate);
        Assert.Equal(expectedDate, repository.AddedEntry?.Date);
    }

    [Fact]
    public async Task GetWaistSummariesQueryHandler_WithDateFromAfterDateTo_ReturnsValidationError() {
        var handler = new GetWaistSummariesQueryHandler(
            new InMemoryWaistEntryRepository(),
            new StubUserRepository(User.Create("user@example.com", "hash")));
        var query = new GetWaistSummariesQuery(Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow.AddDays(-1), 7);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Fact]
    public async Task GetWaistSummariesQueryHandler_WithDateOnlyValues_PreservesRequestedCalendarDates() {
        var repository = new InMemoryWaistEntryRepository();
        var user = User.Create("waist-summary@example.com", "hash");
        var handler = new GetWaistSummariesQueryHandler(repository, new StubUserRepository(user));
        var from = new DateTime(2026, 5, 20, 0, 0, 0, DateTimeKind.Unspecified);
        var to = new DateTime(2026, 5, 21, 0, 0, 0, DateTimeKind.Unspecified);

        var result = await handler.Handle(
            new GetWaistSummariesQuery(user.Id.Value, from, to, 1),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(new DateTime(2026, 5, 20, 0, 0, 0, DateTimeKind.Utc), repository.LastPeriodDateFrom);
        Assert.Equal(new DateTime(2026, 5, 21, 0, 0, 0, DateTimeKind.Utc), repository.LastPeriodDateTo);
    }

    [Fact]
    public async Task GetWaistSummariesQueryHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-waist-summary@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new GetWaistSummariesQueryHandler(
            new InMemoryWaistEntryRepository(),
            new StubUserRepository(user));

        var result = await handler.Handle(
            new GetWaistSummariesQuery(user.Id.Value, DateTime.UtcNow.AddDays(-7), DateTime.UtcNow, 1),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task DeleteWaistEntryCommandHandler_WithEmptyWaistEntryId_ReturnsValidationFailure() {
        var handler = new DeleteWaistEntryCommandHandler(
            new InMemoryWaistEntryRepository(),
            new StubUserRepository(User.Create("user@example.com", "hash")));

        var result = await handler.Handle(
            new DeleteWaistEntryCommand(Guid.NewGuid(), Guid.Empty),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("WaistEntryId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetWaistEntriesQueryHandler_WithDateOnlyRange_PreservesRequestedCalendarDates() {
        var repository = new InMemoryWaistEntryRepository();
        var user = User.Create("waist-list-dateonly@example.com", "hash");
        var handler = new GetWaistEntriesQueryHandler(repository, new StubUserRepository(user));
        var from = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Unspecified);
        var to = new DateTime(2026, 5, 31, 0, 0, 0, DateTimeKind.Unspecified);

        var result = await handler.Handle(
            new GetWaistEntriesQuery(user.Id.Value, from, to, 10, true),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc), repository.LastEntriesDateFrom);
        Assert.Equal(new DateTime(2026, 5, 31, 0, 0, 0, DateTimeKind.Utc), repository.LastEntriesDateTo);
    }

    [Fact]
    public async Task UpdateWaistEntryCommandHandler_WithEmptyWaistEntryId_ReturnsValidationFailure() {
        var handler = new UpdateWaistEntryCommandHandler(
            new InMemoryWaistEntryRepository(),
            new StubUserRepository(User.Create("user@example.com", "hash")));

        var result = await handler.Handle(
            new UpdateWaistEntryCommand(Guid.NewGuid(), Guid.Empty, DateTime.UtcNow, 82),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("WaistEntryId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateWaistEntryCommandHandler_WithDateOnlyValue_PreservesRequestedCalendarDate() {
        var user = User.Create("waist-update-dateonly@example.com", "hash");
        var entry = WaistEntry.Create(user.Id, new DateTime(2026, 5, 26, 0, 0, 0, DateTimeKind.Utc), 82);
        var repository = new InMemoryWaistEntryRepository();
        await repository.AddAsync(entry);
        var handler = new UpdateWaistEntryCommandHandler(repository, new StubUserRepository(user));
        var dateOnly = new DateTime(2026, 5, 27, 0, 0, 0, DateTimeKind.Unspecified);
        var expectedDate = new DateTime(2026, 5, 27, 0, 0, 0, DateTimeKind.Utc);

        var result = await handler.Handle(
            new UpdateWaistEntryCommand(user.Id.Value, entry.Id.Value, dateOnly, 81),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(expectedDate, repository.LastGetByDateDate);
        Assert.Equal(expectedDate, entry.Date);
    }

    [Fact]
    public async Task GetWaistEntriesQueryHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-waist@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new GetWaistEntriesQueryHandler(
            new InMemoryWaistEntryRepository(),
            new StubUserRepository(user));

        var result = await handler.Handle(
            new GetWaistEntriesQuery(user.Id.Value, null, null, 10, true),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task CreateWaistEntryCommandHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-create-waist@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var repository = new InMemoryWaistEntryRepository();
        var handler = new CreateWaistEntryCommandHandler(repository, new StubUserRepository(user));

        var result = await handler.Handle(
            new CreateWaistEntryCommand(user.Id.Value, DateTime.UtcNow, 82),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
        Assert.Null(repository.AddedEntry);
    }

    private static DateTime NormalizeUtcDate(DateTime value) {
        var utc = value.Kind switch {
            DateTimeKind.Utc => value,
            _ => value.ToUniversalTime()
        };

        return DateTime.SpecifyKind(utc.Date, DateTimeKind.Utc);
    }

    [ExcludeFromCodeCoverage]
    private sealed class InMemoryWaistEntryRepository : IWaistEntryRepository {
        private readonly List<WaistEntry> _entries = [];

        public DateTime LastGetByDateDate { get; private set; }
        public DateTime? LastEntriesDateFrom { get; private set; }
        public DateTime? LastEntriesDateTo { get; private set; }
        public DateTime LastPeriodDateFrom { get; private set; }
        public DateTime LastPeriodDateTo { get; private set; }
        public WaistEntry? AddedEntry { get; private set; }

        public Task<WaistEntry> AddAsync(WaistEntry entry, CancellationToken cancellationToken = default) {
            _entries.Add(entry);
            AddedEntry = entry;
            return Task.FromResult(entry);
        }

        public Task UpdateAsync(WaistEntry entry, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task DeleteAsync(WaistEntry entry, CancellationToken cancellationToken = default) {
            _entries.Remove(entry);
            return Task.CompletedTask;
        }

        public Task<WaistEntry?> GetByIdAsync(
            WaistEntryId id,
            UserId userId,
            bool asTracking = false,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_entries.FirstOrDefault(x => x.Id == id && x.UserId == userId));

        public Task<WaistEntry?> GetByDateAsync(
            UserId userId,
            DateTime date,
            CancellationToken cancellationToken = default) {
            LastGetByDateDate = date;
            return Task.FromResult(_entries.FirstOrDefault(x => x.UserId == userId && x.Date == date));
        }

        public Task<IReadOnlyList<WaistEntry>> GetEntriesAsync(
            UserId userId,
            DateTime? dateFrom,
            DateTime? dateTo,
            int? limit,
            bool descending,
            CancellationToken cancellationToken = default) {
            LastEntriesDateFrom = dateFrom;
            LastEntriesDateTo = dateTo;
            IEnumerable<WaistEntry> query = _entries.Where(x => x.UserId == userId);
            if (dateFrom.HasValue) {
                query = query.Where(x => x.Date >= dateFrom.Value);
            }

            if (dateTo.HasValue) {
                query = query.Where(x => x.Date <= dateTo.Value);
            }

            query = descending ? query.OrderByDescending(x => x.Date) : query.OrderBy(x => x.Date);
            if (limit.HasValue) {
                query = query.Take(limit.Value);
            }

            return Task.FromResult<IReadOnlyList<WaistEntry>>(query.ToList());
        }

        public Task<IReadOnlyList<WaistEntry>> GetByPeriodAsync(
            UserId userId,
            DateTime dateFrom,
            DateTime dateTo,
            CancellationToken cancellationToken = default) {
            LastPeriodDateFrom = dateFrom;
            LastPeriodDateTo = dateTo;
            var items = _entries
                .Where(x => x.UserId == userId && x.Date >= dateFrom && x.Date <= dateTo)
                .OrderBy(x => x.Date)
                .ToList();
            return Task.FromResult<IReadOnlyList<WaistEntry>>(items);
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubUserRepository(User user) : IUserRepository {
        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default) => Task.FromResult<User?>(user.Id == id ? user : null);
        public Task<User?> GetByIdIncludingDeletedAsync(UserId id, CancellationToken cancellationToken = default) => Task.FromResult<User?>(user.Id == id ? user : null);
        public Task<User?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByTelegramUserIdIncludingDeletedAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<User> Items, int TotalItems)> GetPagedAsync(string? search, int page, int limit, bool includeDeleted, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<User> RecentUsers)> GetAdminDashboardSummaryAsync(int recentLimit, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Role>> GetRolesByNamesAsync(IReadOnlyList<string> names, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User> AddAsync(User addedUser, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAsync(User updatedUser, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
