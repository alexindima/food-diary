using FoodDiary.Application.Hydration.Commands.CreateHydrationEntry;
using FoodDiary.Application.Hydration.Commands.DeleteHydrationEntry;
using FoodDiary.Application.Hydration.Commands.UpdateHydrationEntry;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Hydration.Common;
using FoodDiary.Application.Hydration.Queries.GetHydrationDailyTotal;
using FoodDiary.Application.Hydration.Queries.GetHydrationEntries;
using FoodDiary.Application.Hydration.Validators;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Hydration;

public class HydrationFeatureTests {
    [Fact]
    public async Task CreateHydrationEntryCommandValidator_WithEmptyUserId_Fails() {
        var validator = new CreateHydrationEntryCommandValidator();
        var command = new CreateHydrationEntryCommand(Guid.Empty, DateTime.UtcNow, 250);

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task DeleteHydrationEntryCommandValidator_WithEmptyEntryId_Fails() {
        var validator = new DeleteHydrationEntryCommandValidator();
        var command = new DeleteHydrationEntryCommand(Guid.NewGuid(), Guid.Empty);

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task UpdateHydrationEntryCommandValidator_WithInvalidAmount_Fails() {
        var validator = new UpdateHydrationEntryCommandValidator();
        var command = new UpdateHydrationEntryCommand(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, 0);

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task GetHydrationDailyTotalQueryValidator_WithValidUserId_Passes() {
        var validator = new GetHydrationDailyTotalQueryValidator();
        var query = new GetHydrationDailyTotalQuery(Guid.NewGuid(), DateTime.UtcNow);

        var result = await validator.ValidateAsync(query);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task GetHydrationEntriesQueryValidator_WithEmptyUserId_Fails() {
        var validator = new GetHydrationEntriesQueryValidator();
        var query = new GetHydrationEntriesQuery(Guid.Empty, DateTime.UtcNow);

        var result = await validator.ValidateAsync(query);

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10001)]
    public void HydrationValidators_ValidateAmount_WithOutOfRangeValue_Fails(int amountMl) {
        var result = HydrationValidators.ValidateAmount(amountMl);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Fact]
    public void HydrationValidators_ValidateAmount_WithValidValue_Passes() {
        var result = HydrationValidators.ValidateAmount(500);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task GetHydrationDailyTotalQueryHandler_WithUnspecifiedDate_PreservesCalendarDayAsUtc() {
        var user = User.Create("user@example.com", "hash");
        var repository = new RecordingHydrationEntryRepository();
        var userRepository = new StubUserRepository(user);
        var handler = new GetHydrationDailyTotalQueryHandler(repository, userRepository);
        var queryDate = new DateTime(2026, 3, 26, 0, 0, 0, DateTimeKind.Unspecified);

        var result = await handler.Handle(
            new GetHydrationDailyTotalQuery(user.Id.Value, queryDate),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(new DateTime(2026, 3, 26, 0, 0, 0, DateTimeKind.Utc), repository.LastDailyTotalDateUtc);
        Assert.Equal(new DateTime(2026, 3, 26, 0, 0, 0, DateTimeKind.Utc), result.Value.DateUtc);
    }

    [Fact]
    public async Task GetHydrationDailyTotalQueryHandler_WithEmptyUserId_ReturnsInvalidToken() {
        var handler = new GetHydrationDailyTotalQueryHandler(
            new RecordingHydrationEntryRepository(),
            new StubUserRepository(User.Create("user@example.com", "hash")));

        var result = await handler.Handle(
            new GetHydrationDailyTotalQuery(Guid.Empty, DateTime.UtcNow),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetHydrationDailyTotalQueryHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-hydration@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new GetHydrationDailyTotalQueryHandler(
            new RecordingHydrationEntryRepository(),
            new StubUserRepository(user));

        var result = await handler.Handle(
            new GetHydrationDailyTotalQuery(user.Id.Value, DateTime.UtcNow),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task DeleteHydrationEntryCommandHandler_WithEmptyHydrationEntryId_ReturnsValidationFailure() {
        var handler = new DeleteHydrationEntryCommandHandler(
            new InMemoryHydrationEntryRepository(),
            new StubUserRepository(User.Create("user@example.com", "hash")));

        var result = await handler.Handle(
            new DeleteHydrationEntryCommand(Guid.NewGuid(), Guid.Empty),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("HydrationEntryId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateHydrationEntryCommandHandler_WithEmptyHydrationEntryId_ReturnsValidationFailure() {
        var handler = new UpdateHydrationEntryCommandHandler(
            new InMemoryHydrationEntryRepository(),
            new StubUserRepository(User.Create("user@example.com", "hash")));

        var result = await handler.Handle(
            new UpdateHydrationEntryCommand(Guid.NewGuid(), Guid.Empty, DateTime.UtcNow, 250),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("HydrationEntryId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class RecordingHydrationEntryRepository : IHydrationEntryRepository {
        public DateTime? LastDailyTotalDateUtc { get; private set; }

        public Task<HydrationEntry> AddAsync(HydrationEntry entry, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task UpdateAsync(HydrationEntry entry, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task DeleteAsync(HydrationEntry entry, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<HydrationEntry?> GetByIdAsync(HydrationEntryId id, bool asTracking = false, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<HydrationEntry>> GetByDateAsync(UserId userId, DateTime dateUtc, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<HydrationEntry>>([]);

        public Task<int> GetDailyTotalAsync(UserId userId, DateTime dateUtc, CancellationToken cancellationToken = default) {
            LastDailyTotalDateUtc = dateUtc;
            return Task.FromResult(0);
        }

        public Task<IReadOnlyList<(DateTime Date, int TotalMl)>> GetDailyTotalsAsync(
            UserId userId, DateTime dateFrom, DateTime dateTo,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

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

    private sealed class InMemoryHydrationEntryRepository : IHydrationEntryRepository {
        public Task<HydrationEntry> AddAsync(HydrationEntry entry, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task UpdateAsync(HydrationEntry entry, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task DeleteAsync(HydrationEntry entry, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<HydrationEntry?> GetByIdAsync(HydrationEntryId id, bool asTracking = false, CancellationToken cancellationToken = default) =>
            Task.FromResult<HydrationEntry?>(null);

        public Task<IReadOnlyList<HydrationEntry>> GetByDateAsync(UserId userId, DateTime dateUtc, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<HydrationEntry>>([]);

        public Task<int> GetDailyTotalAsync(UserId userId, DateTime dateUtc, CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public Task<IReadOnlyList<(DateTime Date, int TotalMl)>> GetDailyTotalsAsync(
            UserId userId, DateTime dateFrom, DateTime dateTo,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<(DateTime, int)>>([]);
    }
}
