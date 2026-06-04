using FoodDiary.Application.WeightEntries.Commands.CreateWeightEntry;
using FoodDiary.Application.WeightEntries.Commands.DeleteWeightEntry;
using FoodDiary.Application.WeightEntries.Commands.UpdateWeightEntry;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.WeightEntries.Common;
using FoodDiary.Application.WeightEntries.Queries.GetLatestWeightEntry;
using FoodDiary.Application.WeightEntries.Queries.GetWeightEntries;
using FoodDiary.Application.WeightEntries.Queries.GetWeightSummaries;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.WeightEntries;

[ExcludeFromCodeCoverage]
public class WeightEntriesFeatureTests {
    [Fact]
    public async Task CreateWeightEntryCommandValidator_WithEmptyUserId_Fails() {
        var validator = new CreateWeightEntryCommandValidator();
        var command = new CreateWeightEntryCommand(Guid.Empty, DateTime.UtcNow, 80);

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task GetWeightEntriesQueryValidator_WithInvalidDateRange_Fails() {
        var validator = new GetWeightEntriesQueryValidator();
        var query = new GetWeightEntriesQuery(Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow.AddDays(-1), 10, true);

        var result = await validator.ValidateAsync(query);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task GetWeightSummariesQueryValidator_WithNonPositiveQuantization_Fails() {
        var validator = new GetWeightSummariesQueryValidator();
        var query = new GetWeightSummariesQuery(Guid.NewGuid(), DateTime.UtcNow.AddDays(-10), DateTime.UtcNow, 0);

        var result = await validator.ValidateAsync(query);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task CreateWeightEntryCommandHandler_WithEmptyUserId_ReturnsInvalidToken() {
        var repository = new InMemoryWeightEntryRepository();
        var handler = new CreateWeightEntryCommandHandler(repository, new StubUserRepository(User.Create("user@example.com", "hash")));

        var result = await handler.Handle(
            new CreateWeightEntryCommand(Guid.Empty, DateTime.UtcNow, 82),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task CreateWeightEntryCommandHandler_NormalizesDateToUtcForDuplicateLookup() {
        var repository = new InMemoryWeightEntryRepository();
        var user = User.Create("weight@example.com", "hash");
        var handler = new CreateWeightEntryCommandHandler(repository, new StubUserRepository(user));
        var userId = user.Id;
        var localDate = new DateTime(2026, 2, 23, 23, 30, 0, DateTimeKind.Local);
        var expectedDate = NormalizeUtcDate(localDate);

        var result = await handler.Handle(
            new CreateWeightEntryCommand(userId.Value, localDate, 82),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(expectedDate, repository.LastGetByDateDate);
        Assert.Equal(DateTimeKind.Utc, repository.LastGetByDateDate.Kind);
    }

    [Fact]
    public async Task CreateWeightEntryCommandHandler_WithDateOnlyValue_PreservesRequestedCalendarDate() {
        var repository = new InMemoryWeightEntryRepository();
        var user = User.Create("weight-dateonly@example.com", "hash");
        var handler = new CreateWeightEntryCommandHandler(repository, new StubUserRepository(user));
        var dateOnly = new DateTime(2026, 5, 27, 0, 0, 0, DateTimeKind.Unspecified);
        var expectedDate = new DateTime(2026, 5, 27, 0, 0, 0, DateTimeKind.Utc);

        var result = await handler.Handle(
            new CreateWeightEntryCommand(user.Id.Value, dateOnly, 82),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(expectedDate, repository.LastGetByDateDate);
        Assert.Equal(expectedDate, repository.AddedEntry?.Date);
    }

    [Fact]
    public async Task GetWeightSummariesQueryHandler_WithDateFromAfterDateTo_ReturnsValidationError() {
        var handler = new GetWeightSummariesQueryHandler(
            new InMemoryWeightEntryRepository(),
            new StubUserRepository(User.Create("user@example.com", "hash")));
        var query = new GetWeightSummariesQuery(Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow.AddDays(-1), 7);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Fact]
    public async Task GetWeightSummariesQueryHandler_WithDateOnlyValues_PreservesRequestedCalendarDates() {
        var repository = new InMemoryWeightEntryRepository();
        var user = User.Create("weight-summary@example.com", "hash");
        var handler = new GetWeightSummariesQueryHandler(repository, new StubUserRepository(user));
        var from = new DateTime(2026, 5, 20, 0, 0, 0, DateTimeKind.Unspecified);
        var to = new DateTime(2026, 5, 21, 0, 0, 0, DateTimeKind.Unspecified);

        var result = await handler.Handle(
            new GetWeightSummariesQuery(user.Id.Value, from, to, 1),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(new DateTime(2026, 5, 20, 0, 0, 0, DateTimeKind.Utc), repository.LastPeriodDateFrom);
        Assert.Equal(new DateTime(2026, 5, 21, 0, 0, 0, DateTimeKind.Utc), repository.LastPeriodDateTo);
    }

    [Fact]
    public async Task GetWeightSummariesQueryHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-weight-summary@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new GetWeightSummariesQueryHandler(
            new InMemoryWeightEntryRepository(),
            new StubUserRepository(user));

        var result = await handler.Handle(
            new GetWeightSummariesQuery(user.Id.Value, DateTime.UtcNow.AddDays(-7), DateTime.UtcNow, 1),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task GetWeightEntriesQueryHandler_WithEmptyUserId_ReturnsInvalidToken() {
        var handler = new GetWeightEntriesQueryHandler(
            new InMemoryWeightEntryRepository(),
            new StubUserRepository(User.Create("user@example.com", "hash")));

        var result = await handler.Handle(
            new GetWeightEntriesQuery(Guid.Empty, null, null, 10, true),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetWeightEntriesQueryHandler_WithDateOnlyRange_PreservesRequestedCalendarDates() {
        var repository = new InMemoryWeightEntryRepository();
        var user = User.Create("weight-list-dateonly@example.com", "hash");
        var handler = new GetWeightEntriesQueryHandler(repository, new StubUserRepository(user));
        var from = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Unspecified);
        var to = new DateTime(2026, 5, 31, 0, 0, 0, DateTimeKind.Unspecified);

        var result = await handler.Handle(
            new GetWeightEntriesQuery(user.Id.Value, from, to, 10, true),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc), repository.LastEntriesDateFrom);
        Assert.Equal(new DateTime(2026, 5, 31, 0, 0, 0, DateTimeKind.Utc), repository.LastEntriesDateTo);
    }

    [Fact]
    public async Task DeleteWeightEntryCommandHandler_WithEmptyWeightEntryId_ReturnsValidationFailure() {
        var handler = new DeleteWeightEntryCommandHandler(
            new InMemoryWeightEntryRepository(),
            new StubUserRepository(User.Create("user@example.com", "hash")));

        var result = await handler.Handle(
            new DeleteWeightEntryCommand(Guid.NewGuid(), Guid.Empty),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("WeightEntryId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteWeightEntryCommandHandler_WithMissingUserId_ReturnsInvalidToken() {
        var handler = new DeleteWeightEntryCommandHandler(
            new InMemoryWeightEntryRepository(),
            new StubUserRepository(User.Create("delete-weight-missing-user@example.com", "hash")));

        var result = await handler.Handle(
            new DeleteWeightEntryCommand(null, WeightEntryId.New().Value),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task DeleteWeightEntryCommandHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("delete-weight-deleted@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new DeleteWeightEntryCommandHandler(
            new InMemoryWeightEntryRepository(),
            new StubUserRepository(user));

        var result = await handler.Handle(
            new DeleteWeightEntryCommand(user.Id.Value, WeightEntryId.New().Value),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task DeleteWeightEntryCommandHandler_WhenEntryMissing_ReturnsNotFound() {
        var user = User.Create("delete-weight-missing-entry@example.com", "hash");
        var handler = new DeleteWeightEntryCommandHandler(
            new InMemoryWeightEntryRepository(),
            new StubUserRepository(user));

        var result = await handler.Handle(
            new DeleteWeightEntryCommand(user.Id.Value, WeightEntryId.New().Value),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("WeightEntry.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task DeleteWeightEntryCommandHandler_WhenEntryExists_DeletesEntry() {
        var user = User.Create("delete-weight-success@example.com", "hash");
        var repository = new InMemoryWeightEntryRepository();
        var entry = await repository.AddAsync(WeightEntry.Create(user.Id, DateTime.UtcNow.Date, 82));
        var handler = new DeleteWeightEntryCommandHandler(repository, new StubUserRepository(user));

        var result = await handler.Handle(
            new DeleteWeightEntryCommand(user.Id.Value, entry.Id.Value),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(await repository.GetByIdAsync(entry.Id, user.Id));
    }

    [Fact]
    public async Task UpdateWeightEntryCommandHandler_WithDateOnlyValue_PreservesRequestedCalendarDate() {
        var user = User.Create("weight-update-dateonly@example.com", "hash");
        var entry = WeightEntry.Create(user.Id, new DateTime(2026, 5, 26, 0, 0, 0, DateTimeKind.Utc), 82);
        var repository = new InMemoryWeightEntryRepository();
        await repository.AddAsync(entry);
        var handler = new UpdateWeightEntryCommandHandler(repository, new StubUserRepository(user));
        var dateOnly = new DateTime(2026, 5, 27, 0, 0, 0, DateTimeKind.Unspecified);
        var expectedDate = new DateTime(2026, 5, 27, 0, 0, 0, DateTimeKind.Utc);

        var result = await handler.Handle(
            new UpdateWeightEntryCommand(user.Id.Value, entry.Id.Value, dateOnly, 81),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(expectedDate, repository.LastGetByDateDate);
        Assert.Equal(expectedDate, entry.Date);
    }

    [Fact]
    public async Task UpdateWeightEntryCommandHandler_WithEmptyWeightEntryId_ReturnsValidationFailure() {
        var handler = new UpdateWeightEntryCommandHandler(
            new InMemoryWeightEntryRepository(),
            new StubUserRepository(User.Create("user@example.com", "hash")));

        var result = await handler.Handle(
            new UpdateWeightEntryCommand(Guid.NewGuid(), Guid.Empty, DateTime.UtcNow, 82),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("WeightEntryId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetWeightEntriesQueryHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-weight@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new GetWeightEntriesQueryHandler(
            new InMemoryWeightEntryRepository(),
            new StubUserRepository(user));

        var result = await handler.Handle(
            new GetWeightEntriesQuery(user.Id.Value, null, null, 10, true),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task CreateWeightEntryCommandHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-create-weight@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var repository = new InMemoryWeightEntryRepository();
        var handler = new CreateWeightEntryCommandHandler(repository, new StubUserRepository(user));

        var result = await handler.Handle(
            new CreateWeightEntryCommand(user.Id.Value, DateTime.UtcNow, 82),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
        Assert.Null(repository.AddedEntry);
    }

    [Fact]
    public async Task GetLatestWeightEntryQueryHandler_WithInvalidUserId_ReturnsInvalidToken() {
        var handler = new GetLatestWeightEntryQueryHandler(
            new InMemoryWeightEntryRepository(),
            new StubUserRepository(User.Create("latest-weight@example.com", "hash")));

        var result = await handler.Handle(new GetLatestWeightEntryQuery(Guid.Empty), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetLatestWeightEntryQueryHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-latest-weight@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new GetLatestWeightEntryQueryHandler(
            new InMemoryWeightEntryRepository(),
            new StubUserRepository(user));

        var result = await handler.Handle(new GetLatestWeightEntryQuery(user.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task GetLatestWeightEntryQueryHandler_ReturnsMostRecentEntry() {
        var user = User.Create("latest-weight-entry@example.com", "hash");
        var repository = new InMemoryWeightEntryRepository();
        await repository.AddAsync(WeightEntry.Create(user.Id, new DateTime(2026, 5, 25, 0, 0, 0, DateTimeKind.Utc), 82));
        var latest = await repository.AddAsync(WeightEntry.Create(user.Id, new DateTime(2026, 5, 27, 0, 0, 0, DateTimeKind.Utc), 81));
        await repository.AddAsync(WeightEntry.Create(UserId.New(), new DateTime(2026, 5, 28, 0, 0, 0, DateTimeKind.Utc), 79));
        var handler = new GetLatestWeightEntryQueryHandler(repository, new StubUserRepository(user));

        var result = await handler.Handle(new GetLatestWeightEntryQuery(user.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(latest.Id.Value, result.Value.Id);
        Assert.Equal(81, result.Value.Weight);
    }

    private static DateTime NormalizeUtcDate(DateTime value) {
        var utc = value.Kind switch {
            DateTimeKind.Utc => value,
            _ => value.ToUniversalTime()
        };

        return DateTime.SpecifyKind(utc.Date, DateTimeKind.Utc);
    }

    [ExcludeFromCodeCoverage]
    private sealed class InMemoryWeightEntryRepository : IWeightEntryRepository {
        private readonly List<WeightEntry> _entries = [];

        public DateTime LastGetByDateDate { get; private set; }
        public DateTime? LastEntriesDateFrom { get; private set; }
        public DateTime? LastEntriesDateTo { get; private set; }
        public DateTime LastPeriodDateFrom { get; private set; }
        public DateTime LastPeriodDateTo { get; private set; }
        public WeightEntry? AddedEntry { get; private set; }

        public Task<WeightEntry> AddAsync(WeightEntry entry, CancellationToken cancellationToken = default) {
            _entries.Add(entry);
            AddedEntry = entry;
            return Task.FromResult(entry);
        }

        public Task UpdateAsync(WeightEntry entry, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task DeleteAsync(WeightEntry entry, CancellationToken cancellationToken = default) {
            _entries.Remove(entry);
            return Task.CompletedTask;
        }

        public Task<WeightEntry?> GetByIdAsync(
            WeightEntryId id,
            UserId userId,
            bool asTracking = false,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_entries.FirstOrDefault(x => x.Id == id && x.UserId == userId));

        public Task<WeightEntry?> GetByDateAsync(
            UserId userId,
            DateTime date,
            CancellationToken cancellationToken = default) {
            LastGetByDateDate = date;
            return Task.FromResult(_entries.FirstOrDefault(x => x.UserId == userId && x.Date == date));
        }

        public Task<IReadOnlyList<WeightEntry>> GetEntriesAsync(
            UserId userId,
            DateTime? dateFrom,
            DateTime? dateTo,
            int? limit,
            bool descending,
            CancellationToken cancellationToken = default) {
            LastEntriesDateFrom = dateFrom;
            LastEntriesDateTo = dateTo;
            IEnumerable<WeightEntry> query = _entries.Where(x => x.UserId == userId);
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

            return Task.FromResult<IReadOnlyList<WeightEntry>>(query.ToList());
        }

        public Task<IReadOnlyList<WeightEntry>> GetByPeriodAsync(
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
            return Task.FromResult<IReadOnlyList<WeightEntry>>(items);
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
