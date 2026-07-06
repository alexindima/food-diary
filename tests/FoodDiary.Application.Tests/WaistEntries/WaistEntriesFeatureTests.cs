using FoodDiary.Application.WaistEntries.Commands.CreateWaistEntry;
using FoodDiary.Application.WaistEntries.Commands.DeleteWaistEntry;
using FoodDiary.Application.WaistEntries.Commands.UpdateWaistEntry;
using FoodDiary.Application.Abstractions.WaistEntries.Common;
using FoodDiary.Application.Abstractions.WaistEntries.Models;
using FoodDiary.Application.WaistEntries.Queries.GetLatestWaistEntry;
using FoodDiary.Application.WaistEntries.Queries.GetWaistEntries;
using FoodDiary.Application.WaistEntries.Queries.GetWaistSummaries;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using FluentValidation.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.WaistEntries.Common;
using FoodDiary.Application.WaistEntries.Mappings;
using FoodDiary.Application.WaistEntries.Models;

namespace FoodDiary.Application.Tests.WaistEntries;

[ExcludeFromCodeCoverage]
public class WaistEntriesFeatureTests {
    [Fact]
    public async Task CreateWaistEntryCommandValidator_WithEmptyUserId_Fails() {
        var validator = new CreateWaistEntryCommandValidator();
        var command = new CreateWaistEntryCommand(Guid.Empty, DateTime.UtcNow, 80);

        ValidationResult result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task GetWaistEntriesQueryValidator_WithInvalidDateRange_Fails() {
        var validator = new GetWaistEntriesQueryValidator();
        var query = new GetWaistEntriesQuery(Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow.AddDays(-1), 10, Descending: true);

        ValidationResult result = await validator.ValidateAsync(query);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task GetWaistSummariesQueryValidator_WithNonPositiveQuantization_Fails() {
        var validator = new GetWaistSummariesQueryValidator();
        var query = new GetWaistSummariesQuery(Guid.NewGuid(), DateTime.UtcNow.AddDays(-10), DateTime.UtcNow, 0);

        ValidationResult result = await validator.ValidateAsync(query);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task CreateWaistEntryCommandHandler_WithEmptyUserId_ReturnsInvalidToken() {
        var repository = new InMemoryWaistEntryRepository();
        var handler = new CreateWaistEntryCommandHandler(repository, CreateCurrentUserAccessService(User.Create("user@example.com", "hash")));

        Result<WaistEntryModel> result = await handler.Handle(
            new CreateWaistEntryCommand(Guid.Empty, DateTime.UtcNow, 82),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task CreateWaistEntryCommandHandler_NormalizesDateToUtcForDuplicateLookup() {
        var repository = new InMemoryWaistEntryRepository();
        var user = User.Create("waist@example.com", "hash");
        var handler = new CreateWaistEntryCommandHandler(repository, CreateCurrentUserAccessService(user));
        UserId userId = user.Id;
        var localDate = new DateTime(2026, 2, 23, 23, 30, 0, DateTimeKind.Local);
        DateTime expectedDate = NormalizeUtcDate(localDate);

        Result<WaistEntryModel> result = await handler.Handle(
            new CreateWaistEntryCommand(userId.Value, localDate, 82),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(expectedDate, repository.LastGetByDateDate);
        Assert.Equal(DateTimeKind.Utc, repository.LastGetByDateDate.Kind);
    }

    [Fact]
    public async Task CreateWaistEntryCommandHandler_WithDateOnlyValue_PreservesRequestedCalendarDate() {
        var repository = new InMemoryWaistEntryRepository();
        var user = User.Create("waist-dateonly@example.com", "hash");
        var handler = new CreateWaistEntryCommandHandler(repository, CreateCurrentUserAccessService(user));
        var dateOnly = new DateTime(2026, 5, 27, 0, 0, 0, DateTimeKind.Unspecified);
        var expectedDate = new DateTime(2026, 5, 27, 0, 0, 0, DateTimeKind.Utc);

        Result<WaistEntryModel> result = await handler.Handle(
            new CreateWaistEntryCommand(user.Id.Value, dateOnly, 82),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(expectedDate, repository.LastGetByDateDate);
        Assert.Equal(expectedDate, repository.AddedEntry?.Date);
    }

    [Fact]
    public async Task GetWaistSummariesQueryHandler_WithDateFromAfterDateTo_ReturnsValidationError() {
        var handler = new GetWaistSummariesQueryHandler(
            new InMemoryWaistEntryRepository(),
            CreateCurrentUserAccessService(User.Create("user@example.com", "hash")));
        var query = new GetWaistSummariesQuery(Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow.AddDays(-1), 7);

        Result<IReadOnlyList<WaistEntrySummaryModel>> result = await handler.Handle(query, CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Fact]
    public async Task GetWaistSummariesQueryHandler_WithMissingUserId_ReturnsInvalidToken() {
        var handler = new GetWaistSummariesQueryHandler(
            new InMemoryWaistEntryRepository(),
            CreateCurrentUserAccessService(User.Create("waist-summary-missing-user@example.com", "hash")));

        Result<IReadOnlyList<WaistEntrySummaryModel>> result = await handler.Handle(
            new GetWaistSummariesQuery(UserId: null, DateTime.UtcNow.AddDays(-7), DateTime.UtcNow, 7),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetWaistSummariesQueryHandler_WithNonPositiveQuantization_ReturnsValidationError() {
        var handler = new GetWaistSummariesQueryHandler(
            new InMemoryWaistEntryRepository(),
            CreateCurrentUserAccessService(User.Create("waist-summary-invalid-step@example.com", "hash")));

        Result<IReadOnlyList<WaistEntrySummaryModel>> result = await handler.Handle(
            new GetWaistSummariesQuery(Guid.NewGuid(), DateTime.UtcNow.AddDays(-7), DateTime.UtcNow, 0),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Fact]
    public async Task GetWaistSummariesQueryHandler_WithDateOnlyValues_PreservesRequestedCalendarDates() {
        var repository = new InMemoryWaistEntryRepository();
        var user = User.Create("waist-summary@example.com", "hash");
        var handler = new GetWaistSummariesQueryHandler(repository, CreateCurrentUserAccessService(user));
        var from = new DateTime(2026, 5, 20, 0, 0, 0, DateTimeKind.Unspecified);
        var to = new DateTime(2026, 5, 21, 0, 0, 0, DateTimeKind.Unspecified);

        Result<IReadOnlyList<WaistEntrySummaryModel>> result = await handler.Handle(
            new GetWaistSummariesQuery(user.Id.Value, from, to, 1),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(new DateTime(2026, 5, 20, 0, 0, 0, DateTimeKind.Utc), repository.LastPeriodDateFrom);
        Assert.Equal(new DateTime(2026, 5, 21, 0, 0, 0, DateTimeKind.Utc), repository.LastPeriodDateTo);
    }

    [Fact]
    public async Task GetWaistSummariesQueryHandler_WhenFinalBucketIsShorter_ReturnsRoundedAverage() {
        var repository = new InMemoryWaistEntryRepository();
        var user = User.Create("waist-summary-average@example.com", "hash");
        await repository.AddAsync(WaistEntry.Create(user.Id, new DateTime(2026, 5, 20, 0, 0, 0, DateTimeKind.Utc), 81.23));
        await repository.AddAsync(WaistEntry.Create(user.Id, new DateTime(2026, 5, 21, 0, 0, 0, DateTimeKind.Utc), 82.78));
        var handler = new GetWaistSummariesQueryHandler(repository, CreateCurrentUserAccessService(user));

        Result<IReadOnlyList<WaistEntrySummaryModel>> result = await handler.Handle(
            new GetWaistSummariesQuery(
                user.Id.Value,
                new DateTime(2026, 5, 20, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 5, 21, 0, 0, 0, DateTimeKind.Utc),
                7),
            CancellationToken.None);

        ResultAssert.Success(result);
        WaistEntrySummaryModel summary = Assert.Single(result.Value);
        Assert.Equal(new DateTime(2026, 5, 20, 0, 0, 0, DateTimeKind.Utc), summary.StartDate);
        Assert.Equal(new DateTime(2026, 5, 21, 0, 0, 0, DateTimeKind.Utc), summary.EndDate);
        Assert.Equal(82, summary.AverageCircumference);
    }

    [Fact]
    public async Task GetWaistSummariesQueryHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-waist-summary@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new GetWaistSummariesQueryHandler(
            new InMemoryWaistEntryRepository(),
            CreateCurrentUserAccessService(user));

        Result<IReadOnlyList<WaistEntrySummaryModel>> result = await handler.Handle(
            new GetWaistSummariesQuery(user.Id.Value, DateTime.UtcNow.AddDays(-7), DateTime.UtcNow, 1),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task DeleteWaistEntryCommandHandler_WithEmptyWaistEntryId_ReturnsValidationFailure() {
        var handler = new DeleteWaistEntryCommandHandler(
            new InMemoryWaistEntryRepository(),
            CreateCurrentUserAccessService(User.Create("user@example.com", "hash")));

        Result result = await handler.Handle(
            new DeleteWaistEntryCommand(Guid.NewGuid(), Guid.Empty),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("WaistEntryId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteWaistEntryCommandHandler_WithMissingUserId_ReturnsInvalidToken() {
        var handler = new DeleteWaistEntryCommandHandler(
            new InMemoryWaistEntryRepository(),
            CreateCurrentUserAccessService(User.Create("delete-waist-missing-user@example.com", "hash")));

        Result result = await handler.Handle(
            new DeleteWaistEntryCommand(UserId: null, WaistEntryId.New().Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task DeleteWaistEntryCommandHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("delete-waist-deleted@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new DeleteWaistEntryCommandHandler(
            new InMemoryWaistEntryRepository(),
            CreateCurrentUserAccessService(user));

        Result result = await handler.Handle(
            new DeleteWaistEntryCommand(user.Id.Value, WaistEntryId.New().Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task DeleteWaistEntryCommandHandler_WhenEntryMissing_ReturnsNotFound() {
        var user = User.Create("delete-waist-missing-entry@example.com", "hash");
        var handler = new DeleteWaistEntryCommandHandler(
            new InMemoryWaistEntryRepository(),
            CreateCurrentUserAccessService(user));

        Result result = await handler.Handle(
            new DeleteWaistEntryCommand(user.Id.Value, WaistEntryId.New().Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("WaistEntry.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task DeleteWaistEntryCommandHandler_WhenEntryExists_DeletesEntry() {
        var user = User.Create("delete-waist-success@example.com", "hash");
        var repository = new InMemoryWaistEntryRepository();
        WaistEntry entry = await repository.AddAsync(WaistEntry.Create(user.Id, DateTime.UtcNow.Date, 82));
        var handler = new DeleteWaistEntryCommandHandler(repository, CreateCurrentUserAccessService(user));

        Result result = await handler.Handle(
            new DeleteWaistEntryCommand(user.Id.Value, entry.Id.Value),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Null(await repository.GetByIdAsync(entry.Id, user.Id));
    }

    [Fact]
    public async Task GetWaistEntriesQueryHandler_WithDateOnlyRange_PreservesRequestedCalendarDates() {
        var repository = new InMemoryWaistEntryRepository();
        var user = User.Create("waist-list-dateonly@example.com", "hash");
        var handler = new GetWaistEntriesQueryHandler(repository, CreateCurrentUserAccessService(user));
        var from = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Unspecified);
        var to = new DateTime(2026, 5, 31, 0, 0, 0, DateTimeKind.Unspecified);

        Result<IReadOnlyList<WaistEntryModel>> result = await handler.Handle(
            new GetWaistEntriesQuery(user.Id.Value, from, to, 10, Descending: true),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc), repository.LastEntriesDateFrom);
        Assert.Equal(new DateTime(2026, 5, 31, 0, 0, 0, DateTimeKind.Utc), repository.LastEntriesDateTo);
    }

    [Fact]
    public async Task GetWaistEntriesQueryHandler_WithEntries_ReturnsMappedEntries() {
        var repository = new InMemoryWaistEntryRepository();
        var user = User.Create("waist-list-mapped@example.com", "hash");
        WaistEntry older = await repository.AddAsync(WaistEntry.Create(
            user.Id,
            new DateTime(2026, 5, 20, 0, 0, 0, DateTimeKind.Utc),
            82));
        WaistEntry newer = await repository.AddAsync(WaistEntry.Create(
            user.Id,
            new DateTime(2026, 5, 21, 0, 0, 0, DateTimeKind.Utc),
            81));
        await repository.AddAsync(WaistEntry.Create(UserId.New(), newer.Date.AddDays(1), 79));
        var handler = new GetWaistEntriesQueryHandler(repository, CreateCurrentUserAccessService(user));

        Result<IReadOnlyList<WaistEntryModel>> result = await handler.Handle(
            new GetWaistEntriesQuery(user.Id.Value, DateFrom: null, DateTo: null, 10, Descending: true),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Collection(
            result.Value,
            entry => {
                Assert.Equal(newer.Id.Value, entry.Id);
                Assert.Equal(81, entry.Circumference);
            },
            entry => {
                Assert.Equal(older.Id.Value, entry.Id);
                Assert.Equal(82, entry.Circumference);
            });
    }

    [Fact]
    public async Task GetWaistEntriesQueryHandler_WithMissingUserId_ReturnsInvalidToken() {
        var handler = new GetWaistEntriesQueryHandler(
            new InMemoryWaistEntryRepository(),
            CreateCurrentUserAccessService(User.Create("waist-list-missing-user@example.com", "hash")));

        Result<IReadOnlyList<WaistEntryModel>> result = await handler.Handle(
            new GetWaistEntriesQuery(UserId: null, DateFrom: null, DateTo: null, 10, Descending: true),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task UpdateWaistEntryCommandHandler_WithEmptyWaistEntryId_ReturnsValidationFailure() {
        var handler = new UpdateWaistEntryCommandHandler(
            new InMemoryWaistEntryRepository(),
            CreateCurrentUserAccessService(User.Create("user@example.com", "hash")));

        Result<WaistEntryModel> result = await handler.Handle(
            new UpdateWaistEntryCommand(Guid.NewGuid(), Guid.Empty, DateTime.UtcNow, 82),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("WaistEntryId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateWaistEntryCommandHandler_WithMissingUserId_ReturnsInvalidToken() {
        var handler = new UpdateWaistEntryCommandHandler(
            new InMemoryWaistEntryRepository(),
            CreateCurrentUserAccessService(User.Create("waist-update-missing-user@example.com", "hash")));

        Result<WaistEntryModel> result = await handler.Handle(
            new UpdateWaistEntryCommand(UserId: null, WaistEntryId.New().Value, DateTime.UtcNow, 82),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task UpdateWaistEntryCommandHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("waist-update-deleted-user@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new UpdateWaistEntryCommandHandler(
            new InMemoryWaistEntryRepository(),
            CreateCurrentUserAccessService(user));

        Result<WaistEntryModel> result = await handler.Handle(
            new UpdateWaistEntryCommand(user.Id.Value, WaistEntryId.New().Value, DateTime.UtcNow, 82),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task UpdateWaistEntryCommandHandler_WhenEntryMissing_ReturnsNotFound() {
        var user = User.Create("waist-update-missing-entry@example.com", "hash");
        var handler = new UpdateWaistEntryCommandHandler(
            new InMemoryWaistEntryRepository(),
            CreateCurrentUserAccessService(user));

        Result<WaistEntryModel> result = await handler.Handle(
            new UpdateWaistEntryCommand(user.Id.Value, WaistEntryId.New().Value, DateTime.UtcNow, 82),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("WaistEntry.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task UpdateWaistEntryCommandHandler_WhenAnotherEntryExistsForDate_ReturnsAlreadyExists() {
        var user = User.Create("waist-update-duplicate@example.com", "hash");
        var repository = new InMemoryWaistEntryRepository();
        WaistEntry entry = await repository.AddAsync(WaistEntry.Create(
            user.Id,
            new DateTime(2026, 5, 26, 0, 0, 0, DateTimeKind.Utc),
            82));
        WaistEntry duplicate = await repository.AddAsync(WaistEntry.Create(
            user.Id,
            new DateTime(2026, 5, 27, 0, 0, 0, DateTimeKind.Utc),
            81));
        var handler = new UpdateWaistEntryCommandHandler(repository, CreateCurrentUserAccessService(user));

        Result<WaistEntryModel> result = await handler.Handle(
            new UpdateWaistEntryCommand(user.Id.Value, entry.Id.Value, duplicate.Date, 80),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("WaistEntry.AlreadyExists", result.Error.Code);
    }

    [Fact]
    public async Task UpdateWaistEntryCommandHandler_WithDateOnlyValue_PreservesRequestedCalendarDate() {
        var user = User.Create("waist-update-dateonly@example.com", "hash");
        var entry = WaistEntry.Create(user.Id, new DateTime(2026, 5, 26, 0, 0, 0, DateTimeKind.Utc), 82);
        var repository = new InMemoryWaistEntryRepository();
        await repository.AddAsync(entry);
        var handler = new UpdateWaistEntryCommandHandler(repository, CreateCurrentUserAccessService(user));
        var dateOnly = new DateTime(2026, 5, 27, 0, 0, 0, DateTimeKind.Unspecified);
        var expectedDate = new DateTime(2026, 5, 27, 0, 0, 0, DateTimeKind.Utc);

        Result<WaistEntryModel> result = await handler.Handle(
            new UpdateWaistEntryCommand(user.Id.Value, entry.Id.Value, dateOnly, 81),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(expectedDate, repository.LastGetByDateDate);
        Assert.Equal(expectedDate, entry.Date);
    }

    [Fact]
    public async Task GetWaistEntriesQueryHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-waist@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new GetWaistEntriesQueryHandler(
            new InMemoryWaistEntryRepository(),
            CreateCurrentUserAccessService(user));

        Result<IReadOnlyList<WaistEntryModel>> result = await handler.Handle(
            new GetWaistEntriesQuery(user.Id.Value, DateFrom: null, DateTo: null, 10, Descending: true),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task CreateWaistEntryCommandHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-create-waist@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var repository = new InMemoryWaistEntryRepository();
        var handler = new CreateWaistEntryCommandHandler(repository, CreateCurrentUserAccessService(user));

        Result<WaistEntryModel> result = await handler.Handle(
            new CreateWaistEntryCommand(user.Id.Value, DateTime.UtcNow, 82),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
        Assert.Null(repository.AddedEntry);
    }

    [Fact]
    public async Task GetLatestWaistEntryQueryHandler_WithMissingUserId_ReturnsInvalidToken() {
        var handler = new GetLatestWaistEntryQueryHandler(
            new InMemoryWaistEntryRepository(),
            CreateCurrentUserAccessService(User.Create("latest-waist@example.com", "hash")));

        Result<WaistEntryModel?> result = await handler.Handle(new GetLatestWaistEntryQuery(UserId: null), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetLatestWaistEntryQueryHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-latest-waist@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new GetLatestWaistEntryQueryHandler(
            new InMemoryWaistEntryRepository(),
            CreateCurrentUserAccessService(user));

        Result<WaistEntryModel?> result = await handler.Handle(new GetLatestWaistEntryQuery(user.Id.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task GetLatestWaistEntryQueryHandler_ReturnsMostRecentEntry() {
        var user = User.Create("latest-waist-entry@example.com", "hash");
        var repository = new InMemoryWaistEntryRepository();
        await repository.AddAsync(WaistEntry.Create(user.Id, new DateTime(2026, 5, 25, 0, 0, 0, DateTimeKind.Utc), 82));
        WaistEntry latest = await repository.AddAsync(WaistEntry.Create(user.Id, new DateTime(2026, 5, 27, 0, 0, 0, DateTimeKind.Utc), 81));
        await repository.AddAsync(WaistEntry.Create(UserId.New(), new DateTime(2026, 5, 28, 0, 0, 0, DateTimeKind.Utc), 79));
        var handler = new GetLatestWaistEntryQueryHandler(repository, CreateCurrentUserAccessService(user));

        Result<WaistEntryModel?> result = await handler.Handle(new GetLatestWaistEntryQuery(user.Id.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.NotNull(result.Value);
        Assert.Equal(latest.Id.Value, result.Value.Id);
        Assert.Equal(81, result.Value.Circumference);
    }

    private static DateTime NormalizeUtcDate(DateTime value) {
        DateTime utc = value.Kind switch {
            DateTimeKind.Utc => value,
            _ => value.ToUniversalTime(),
        };

        return DateTime.SpecifyKind(utc.Date, DateTimeKind.Utc);
    }

    [ExcludeFromCodeCoverage]
    private sealed class InMemoryWaistEntryRepository : IWaistEntryRepository, IWaistEntryReadService {
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

        public async Task<IReadOnlyList<WaistEntryReadModel>> GetEntryReadModelsAsync(
            UserId userId,
            DateTime? dateFrom,
            DateTime? dateTo,
            int? limit,
            bool descending,
            CancellationToken cancellationToken = default) {
            IReadOnlyList<WaistEntry> entries = await GetEntriesAsync(
                userId,
                dateFrom,
                dateTo,
                limit,
                descending,
                cancellationToken).ConfigureAwait(false);

            return [.. entries.Select(entry => new WaistEntryReadModel(entry.Id.Value, entry.UserId.Value, entry.Date, entry.Circumference))];
        }

        public async Task<IReadOnlyList<WaistEntryReadModel>> GetByPeriodReadModelsAsync(
            UserId userId,
            DateTime dateFrom,
            DateTime dateTo,
            CancellationToken cancellationToken = default) {
            IReadOnlyList<WaistEntry> entries = await GetByPeriodAsync(userId, dateFrom, dateTo, cancellationToken).ConfigureAwait(false);
            return [.. entries.Select(entry => new WaistEntryReadModel(entry.Id.Value, entry.UserId.Value, entry.Date, entry.Circumference))];
        }

        async Task<IReadOnlyList<WaistEntryModel>> IWaistEntryReadService.GetEntriesAsync(
            UserId userId,
            DateTime? dateFrom,
            DateTime? dateTo,
            int? limit,
            bool descending,
            CancellationToken cancellationToken) {
            IReadOnlyList<WaistEntry> entries = await GetEntriesAsync(
                userId,
                dateFrom,
                dateTo,
                limit,
                descending,
                cancellationToken).ConfigureAwait(false);

            return [.. entries.Select(entry => entry.ToModel())];
        }

        async Task<WaistEntryModel?> IWaistEntryReadService.GetLatestAsync(UserId userId, CancellationToken cancellationToken) {
            IReadOnlyList<WaistEntryModel> entries = await ((IWaistEntryReadService)this)
                .GetEntriesAsync(userId, dateFrom: null, dateTo: null, limit: 1, descending: true, cancellationToken)
                .ConfigureAwait(false);

            return entries.Count > 0 ? entries[0] : null;
        }

        async Task<IReadOnlyList<WaistEntrySummaryModel>> IWaistEntryReadService.GetSummariesAsync(
            UserId userId,
            DateTime dateFrom,
            DateTime dateTo,
            int quantizationDays,
            CancellationToken cancellationToken) {
            IReadOnlyList<WaistEntry> entries = await GetByPeriodAsync(userId, dateFrom, dateTo, cancellationToken).ConfigureAwait(false);
            return [.. BuildBuckets(dateFrom, dateTo, quantizationDays).Select(bucket => BuildResponse(bucket.start, bucket.end, entries))];
        }

        private static IEnumerable<(DateTime start, DateTime end)> BuildBuckets(DateTime from, DateTime to, int step) {
            DateTime current = from.Date;
            DateTime end = to.Date;
            while (current <= end) {
                DateTime bucketEnd = current.AddDays(step - 1);
                if (bucketEnd > end) {
                    bucketEnd = end;
                }

                yield return (current, bucketEnd);
                current = bucketEnd.AddDays(1);
            }
        }

        private static WaistEntrySummaryModel BuildResponse(
            DateTime start,
            DateTime end,
            IReadOnlyList<WaistEntry> entries) {
            List<WaistEntry> bucketEntries = [.. entries.Where(entry => entry.Date >= start && entry.Date <= end)];

            if (bucketEntries.Count == 0) {
                return new WaistEntrySummaryModel(start, end, 0);
            }

            double avg = bucketEntries.Average(entry => entry.Circumference);
            return new WaistEntrySummaryModel(start, end, Math.Round(avg, 2, MidpointRounding.ToEven));
        }
    }

    private static ICurrentUserAccessService CreateCurrentUserAccessService(User user) {
        ICurrentUserAccessService service = Substitute.For<ICurrentUserAccessService>();
        Error? error = user.DeletedAt is null ? null : Errors.Authentication.AccountDeleted;
        service
            .EnsureCanAccessAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                UserId id = call.Arg<UserId>();
                return Task.FromResult(user.Id == id ? error : Errors.Authentication.InvalidToken);
            });
        return service;
    }
}
