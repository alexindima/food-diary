using FoodDiary.Application.Hydration.Commands.CreateHydrationEntry;
using FoodDiary.Application.Hydration.Commands.DeleteHydrationEntry;
using FoodDiary.Application.Hydration.Commands.UpdateHydrationEntry;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Hydration.Common;
using FoodDiary.Application.Hydration.Queries.GetHydrationDailyTotal;
using FoodDiary.Application.Hydration.Queries.GetHydrationEntries;
using FoodDiary.Application.Hydration.Validators;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using FluentValidation.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Hydration.Models;

namespace FoodDiary.Application.Tests.Hydration;

[ExcludeFromCodeCoverage]
public class HydrationFeatureTests {
    [Fact]
    public async Task CreateHydrationEntryCommandValidator_WithEmptyUserId_Fails() {
        var validator = new CreateHydrationEntryCommandValidator();
        var command = new CreateHydrationEntryCommand(Guid.Empty, DateTime.UtcNow, 250);

        ValidationResult result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task DeleteHydrationEntryCommandValidator_WithEmptyEntryId_Fails() {
        var validator = new DeleteHydrationEntryCommandValidator();
        var command = new DeleteHydrationEntryCommand(Guid.NewGuid(), Guid.Empty);

        ValidationResult result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task UpdateHydrationEntryCommandValidator_WithInvalidAmount_Fails() {
        var validator = new UpdateHydrationEntryCommandValidator();
        var command = new UpdateHydrationEntryCommand(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, 0);

        ValidationResult result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task GetHydrationDailyTotalQueryValidator_WithValidUserId_Passes() {
        var validator = new GetHydrationDailyTotalQueryValidator();
        var query = new GetHydrationDailyTotalQuery(Guid.NewGuid(), DateTime.UtcNow);

        ValidationResult result = await validator.ValidateAsync(query);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task GetHydrationEntriesQueryValidator_WithEmptyUserId_Fails() {
        var validator = new GetHydrationEntriesQueryValidator();
        var query = new GetHydrationEntriesQuery(Guid.Empty, DateTime.UtcNow);

        ValidationResult result = await validator.ValidateAsync(query);

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10001)]
    public void HydrationValidators_ValidateAmount_WithOutOfRangeValue_Fails(int amountMl) {
        Result result = HydrationValidators.ValidateAmount(amountMl);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Fact]
    public void HydrationValidators_ValidateAmount_WithValidValue_Passes() {
        Result result = HydrationValidators.ValidateAmount(500);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task GetHydrationDailyTotalQueryHandler_WithUnspecifiedDate_PreservesCalendarDayAsUtc() {
        var user = User.Create("user@example.com", "hash");
        var repository = new RecordingHydrationEntryRepository();
        IUserRepository userRepository = CreateUserRepository(user);
        var handler = new GetHydrationDailyTotalQueryHandler(repository, userRepository);
        var queryDate = new DateTime(2026, 3, 26, 0, 0, 0, DateTimeKind.Unspecified);

        Result<HydrationDailyModel> result = await handler.Handle(
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
            CreateUserRepository(User.Create("user@example.com", "hash")));

        Result<HydrationDailyModel> result = await handler.Handle(
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
            CreateUserRepository(user));

        Result<HydrationDailyModel> result = await handler.Handle(
            new GetHydrationDailyTotalQuery(user.Id.Value, DateTime.UtcNow),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task CreateHydrationEntryCommandHandler_WithUnspecifiedTimestamp_PreservesInstantAsUtc() {
        var user = User.Create("hydration-create@example.com", "hash");
        var repository = new InMemoryHydrationEntryRepository();
        var handler = new CreateHydrationEntryCommandHandler(repository, CreateUserRepository(user));
        var timestamp = new DateTime(2026, 3, 26, 14, 30, 0, DateTimeKind.Unspecified);

        Result<HydrationEntryModel> result = await handler.Handle(
            new CreateHydrationEntryCommand(user.Id.Value, timestamp, 250),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(DateTimeKind.Utc, result.Value.TimestampUtc.Kind);
        Assert.Equal(DateTime.SpecifyKind(timestamp, DateTimeKind.Utc), result.Value.TimestampUtc);
        Assert.Equal(result.Value.TimestampUtc, repository.AddedEntry?.Timestamp);
    }

    [Fact]
    public async Task CreateHydrationEntryCommandHandler_WithEmptyUserId_ReturnsInvalidToken() {
        var handler = new CreateHydrationEntryCommandHandler(
            new InMemoryHydrationEntryRepository(),
            CreateUserRepository(User.Create("hydration-create-empty@example.com", "hash")));

        Result<HydrationEntryModel> result = await handler.Handle(
            new CreateHydrationEntryCommand(Guid.Empty, DateTime.UtcNow, 250),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task CreateHydrationEntryCommandHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("hydration-create-deleted@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new CreateHydrationEntryCommandHandler(
            new InMemoryHydrationEntryRepository(),
            CreateUserRepository(user));

        Result<HydrationEntryModel> result = await handler.Handle(
            new CreateHydrationEntryCommand(user.Id.Value, DateTime.UtcNow, 250),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task CreateHydrationEntryCommandHandler_WithInvalidAmount_ReturnsValidationFailure() {
        var user = User.Create("hydration-create-invalid@example.com", "hash");
        var handler = new CreateHydrationEntryCommandHandler(
            new InMemoryHydrationEntryRepository(),
            CreateUserRepository(user));

        Result<HydrationEntryModel> result = await handler.Handle(
            new CreateHydrationEntryCommand(user.Id.Value, DateTime.UtcNow, 0),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Fact]
    public async Task UpdateHydrationEntryCommandHandler_WithUnspecifiedTimestamp_PreservesInstantAsUtc() {
        var user = User.Create("hydration-update@example.com", "hash");
        var entry = HydrationEntry.Create(user.Id, new DateTime(2026, 3, 25, 12, 0, 0, DateTimeKind.Utc), 250);
        var repository = new InMemoryHydrationEntryRepository(entry);
        var handler = new UpdateHydrationEntryCommandHandler(repository, CreateUserRepository(user));
        var timestamp = new DateTime(2026, 3, 26, 14, 30, 0, DateTimeKind.Unspecified);

        Result<HydrationEntryModel> result = await handler.Handle(
            new UpdateHydrationEntryCommand(user.Id.Value, entry.Id.Value, timestamp, AmountMl: null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(DateTimeKind.Utc, result.Value.TimestampUtc.Kind);
        Assert.Equal(DateTime.SpecifyKind(timestamp, DateTimeKind.Utc), result.Value.TimestampUtc);
    }

    [Fact]
    public async Task UpdateHydrationEntryCommandHandler_WithValidAmount_UpdatesEntry() {
        var user = User.Create("hydration-update-amount@example.com", "hash");
        var entry = HydrationEntry.Create(user.Id, DateTime.UtcNow.AddHours(-1), 250);
        var repository = new InMemoryHydrationEntryRepository(entry);
        var handler = new UpdateHydrationEntryCommandHandler(repository, CreateUserRepository(user));

        Result<HydrationEntryModel> result = await handler.Handle(
            new UpdateHydrationEntryCommand(user.Id.Value, entry.Id.Value, DateTime.UtcNow, 750),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(750, result.Value.AmountMl);
    }

    [Fact]
    public async Task UpdateHydrationEntryCommandHandler_WithEmptyUserId_ReturnsInvalidToken() {
        var handler = new UpdateHydrationEntryCommandHandler(
            new InMemoryHydrationEntryRepository(),
            CreateUserRepository(User.Create("hydration-update-empty@example.com", "hash")));

        Result<HydrationEntryModel> result = await handler.Handle(
            new UpdateHydrationEntryCommand(Guid.Empty, Guid.NewGuid(), DateTime.UtcNow, 250),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task UpdateHydrationEntryCommandHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("hydration-update-deleted@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var entry = HydrationEntry.Create(user.Id, DateTime.UtcNow, 250);
        var handler = new UpdateHydrationEntryCommandHandler(
            new InMemoryHydrationEntryRepository(entry),
            CreateUserRepository(user));

        Result<HydrationEntryModel> result = await handler.Handle(
            new UpdateHydrationEntryCommand(user.Id.Value, entry.Id.Value, DateTime.UtcNow, 500),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task UpdateHydrationEntryCommandHandler_WhenEntryMissing_ReturnsNotFound() {
        var user = User.Create("hydration-update-missing@example.com", "hash");
        var handler = new UpdateHydrationEntryCommandHandler(
            new InMemoryHydrationEntryRepository(),
            CreateUserRepository(user));
        var entryId = Guid.NewGuid();

        Result<HydrationEntryModel> result = await handler.Handle(
            new UpdateHydrationEntryCommand(user.Id.Value, entryId, DateTime.UtcNow, 500),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("HydrationEntry.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task UpdateHydrationEntryCommandHandler_WithEntryFromOtherUser_ReturnsNotFound() {
        var user = User.Create("hydration-update-owner@example.com", "hash");
        var entry = HydrationEntry.Create(UserId.New(), DateTime.UtcNow, 250);
        var handler = new UpdateHydrationEntryCommandHandler(
            new InMemoryHydrationEntryRepository(entry),
            CreateUserRepository(user));

        Result<HydrationEntryModel> result = await handler.Handle(
            new UpdateHydrationEntryCommand(user.Id.Value, entry.Id.Value, DateTime.UtcNow, 500),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("HydrationEntry.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task UpdateHydrationEntryCommandHandler_WithInvalidAmount_ReturnsValidationFailure() {
        var user = User.Create("hydration-update-invalid@example.com", "hash");
        var entry = HydrationEntry.Create(user.Id, DateTime.UtcNow, 250);
        var handler = new UpdateHydrationEntryCommandHandler(
            new InMemoryHydrationEntryRepository(entry),
            CreateUserRepository(user));

        Result<HydrationEntryModel> result = await handler.Handle(
            new UpdateHydrationEntryCommand(user.Id.Value, entry.Id.Value, DateTime.UtcNow, 0),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Fact]
    public async Task DeleteHydrationEntryCommandHandler_WithEmptyHydrationEntryId_ReturnsValidationFailure() {
        var handler = new DeleteHydrationEntryCommandHandler(
            new InMemoryHydrationEntryRepository(),
            CreateUserRepository(User.Create("user@example.com", "hash")));

        Result result = await handler.Handle(
            new DeleteHydrationEntryCommand(Guid.NewGuid(), Guid.Empty),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("HydrationEntryId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteHydrationEntryCommandHandler_WithEmptyUserId_ReturnsInvalidToken() {
        var handler = new DeleteHydrationEntryCommandHandler(
            new InMemoryHydrationEntryRepository(),
            CreateUserRepository(User.Create("hydration-delete-empty@example.com", "hash")));

        Result result = await handler.Handle(
            new DeleteHydrationEntryCommand(Guid.Empty, Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task DeleteHydrationEntryCommandHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("hydration-delete-deleted@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var entry = HydrationEntry.Create(user.Id, DateTime.UtcNow, 250);
        var handler = new DeleteHydrationEntryCommandHandler(
            new InMemoryHydrationEntryRepository(entry),
            CreateUserRepository(user));

        Result result = await handler.Handle(
            new DeleteHydrationEntryCommand(user.Id.Value, entry.Id.Value),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task DeleteHydrationEntryCommandHandler_WhenEntryMissing_ReturnsNotFound() {
        var user = User.Create("hydration-delete-missing@example.com", "hash");
        var handler = new DeleteHydrationEntryCommandHandler(
            new InMemoryHydrationEntryRepository(),
            CreateUserRepository(user));

        Result result = await handler.Handle(
            new DeleteHydrationEntryCommand(user.Id.Value, Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("HydrationEntry.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task DeleteHydrationEntryCommandHandler_WithEntryFromOtherUser_ReturnsNotFound() {
        var user = User.Create("hydration-delete-owner@example.com", "hash");
        var entry = HydrationEntry.Create(UserId.New(), DateTime.UtcNow, 250);
        var handler = new DeleteHydrationEntryCommandHandler(
            new InMemoryHydrationEntryRepository(entry),
            CreateUserRepository(user));

        Result result = await handler.Handle(
            new DeleteHydrationEntryCommand(user.Id.Value, entry.Id.Value),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("HydrationEntry.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task DeleteHydrationEntryCommandHandler_WithOwnedEntry_DeletesEntry() {
        var user = User.Create("hydration-delete-success@example.com", "hash");
        var entry = HydrationEntry.Create(user.Id, DateTime.UtcNow, 250);
        var repository = new InMemoryHydrationEntryRepository(entry);
        var handler = new DeleteHydrationEntryCommandHandler(repository, CreateUserRepository(user));

        Result result = await handler.Handle(
            new DeleteHydrationEntryCommand(user.Id.Value, entry.Id.Value),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Same(entry, repository.DeletedEntry);
    }

    [Fact]
    public async Task UpdateHydrationEntryCommandHandler_WithEmptyHydrationEntryId_ReturnsValidationFailure() {
        var handler = new UpdateHydrationEntryCommandHandler(
            new InMemoryHydrationEntryRepository(),
            CreateUserRepository(User.Create("user@example.com", "hash")));

        Result<HydrationEntryModel> result = await handler.Handle(
            new UpdateHydrationEntryCommand(Guid.NewGuid(), Guid.Empty, DateTime.UtcNow, 250),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("HydrationEntryId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetHydrationEntriesQueryHandler_WithInvalidUserId_ReturnsInvalidToken() {
        var handler = new GetHydrationEntriesQueryHandler(
            new InMemoryHydrationEntryRepository(),
            CreateUserRepository(User.Create("hydration-entries@example.com", "hash")));

        Result<IReadOnlyList<HydrationEntryModel>> result = await handler.Handle(new GetHydrationEntriesQuery(Guid.Empty, DateTime.UtcNow), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetHydrationEntriesQueryHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-hydration-entries@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new GetHydrationEntriesQueryHandler(
            new InMemoryHydrationEntryRepository(),
            CreateUserRepository(user));

        Result<IReadOnlyList<HydrationEntryModel>> result = await handler.Handle(new GetHydrationEntriesQuery(user.Id.Value, DateTime.UtcNow), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task GetHydrationEntriesQueryHandler_NormalizesDateAndMapsEntries() {
        var user = User.Create("hydration-entries-date@example.com", "hash");
        var repository = new InMemoryHydrationEntryRepository();
        HydrationEntry entry = await repository.AddAsync(HydrationEntry.Create(
            user.Id,
            new DateTime(2026, 5, 27, 0, 0, 0, DateTimeKind.Utc),
            350));
        await repository.AddAsync(HydrationEntry.Create(
            UserId.New(),
            new DateTime(2026, 5, 27, 0, 0, 0, DateTimeKind.Utc),
            250));
        var handler = new GetHydrationEntriesQueryHandler(repository, CreateUserRepository(user));
        var dateOnly = new DateTime(2026, 5, 27, 0, 0, 0, DateTimeKind.Unspecified);

        Result<IReadOnlyList<HydrationEntryModel>> result = await handler.Handle(new GetHydrationEntriesQuery(user.Id.Value, dateOnly), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(new DateTime(2026, 5, 27, 0, 0, 0, DateTimeKind.Utc), repository.LastGetByDateDateUtc);
        HydrationEntryModel model = Assert.Single(result.Value);
        Assert.Equal(entry.Id.Value, model.Id);
        Assert.Equal(350, model.AmountMl);
    }

    [ExcludeFromCodeCoverage]
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

    private static IUserRepository CreateUserRepository(User user) {
        IUserRepository repository = Substitute.For<IUserRepository>();
        repository
            .GetByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                UserId id = call.Arg<UserId>();
                return Task.FromResult<User?>(user.Id == id ? user : null);
            });
        repository
            .GetByIdIncludingDeletedAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                UserId id = call.Arg<UserId>();
                return Task.FromResult<User?>(user.Id == id ? user : null);
            });
        return repository;
    }

    [ExcludeFromCodeCoverage]
    private sealed class InMemoryHydrationEntryRepository(HydrationEntry? entry = null) : IHydrationEntryRepository {
        private HydrationEntry? _entry = entry;
        private readonly List<HydrationEntry> _entries = entry is null ? [] : [entry];
        public HydrationEntry? AddedEntry { get; private set; }
        public HydrationEntry? DeletedEntry { get; private set; }
        public DateTime? LastGetByDateDateUtc { get; private set; }

        public Task<HydrationEntry> AddAsync(HydrationEntry entry, CancellationToken cancellationToken = default) {
            AddedEntry = entry;
            _entry = entry;
            _entries.Add(entry);
            return Task.FromResult(entry);
        }

        public Task UpdateAsync(HydrationEntry entry, CancellationToken cancellationToken = default) {
            _entry = entry;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(HydrationEntry entry, CancellationToken cancellationToken = default) {
            DeletedEntry = entry;
            _entry = null;
            _entries.Remove(entry);
            return Task.CompletedTask;
        }

        public Task<HydrationEntry?> GetByIdAsync(HydrationEntryId id, bool asTracking = false, CancellationToken cancellationToken = default) =>
            Task.FromResult(_entry?.Id == id ? _entry : null);

        public Task<IReadOnlyList<HydrationEntry>> GetByDateAsync(UserId userId, DateTime dateUtc, CancellationToken cancellationToken = default) {
            LastGetByDateDateUtc = dateUtc;
            var entries = _entries
                .Where(entry => entry.UserId == userId && entry.Timestamp.Date == dateUtc.Date)
                .ToList();
            return Task.FromResult<IReadOnlyList<HydrationEntry>>(entries);
        }

        public Task<int> GetDailyTotalAsync(UserId userId, DateTime dateUtc, CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public Task<IReadOnlyList<(DateTime Date, int TotalMl)>> GetDailyTotalsAsync(
            UserId userId, DateTime dateFrom, DateTime dateTo,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<(DateTime, int)>>([]);
    }
}
