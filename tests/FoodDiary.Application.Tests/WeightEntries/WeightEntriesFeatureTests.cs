using FoodDiary.Application.WeightEntries.Commands.CreateWeightEntry;
using FoodDiary.Application.WeightEntries.Commands.DeleteWeightEntry;
using FoodDiary.Application.WeightEntries.Commands.UpdateWeightEntry;
using FoodDiary.Application.Abstractions.WeightEntries.Common;
using FoodDiary.Application.Abstractions.WeightEntries.Models;
using FoodDiary.Application.WeightEntries.Queries.GetLatestWeightEntry;
using FoodDiary.Application.WeightEntries.Queries.GetWeightEntries;
using FoodDiary.Application.WeightEntries.Queries.GetWeightSummaries;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using FluentValidation.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.WeightEntries.Common;
using FoodDiary.Application.WeightEntries.Mappings;
using FoodDiary.Application.WeightEntries.Models;

namespace FoodDiary.Application.Tests.WeightEntries;

[ExcludeFromCodeCoverage]
public class WeightEntriesFeatureTests {
    [Fact]
    public async Task CreateWeightEntryCommandValidator_WithEmptyUserId_Fails() {
        var validator = new CreateWeightEntryCommandValidator();
        var command = new CreateWeightEntryCommand(Guid.Empty, DateTime.UtcNow, 80);

        ValidationResult result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task GetWeightEntriesQueryValidator_WithInvalidDateRange_Fails() {
        var validator = new GetWeightEntriesQueryValidator();
        var query = new GetWeightEntriesQuery(Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow.AddDays(-1), 10, Descending: true);

        ValidationResult result = await validator.ValidateAsync(query);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task GetWeightSummariesQueryValidator_WithNonPositiveQuantization_Fails() {
        var validator = new GetWeightSummariesQueryValidator();
        var query = new GetWeightSummariesQuery(Guid.NewGuid(), DateTime.UtcNow.AddDays(-10), DateTime.UtcNow, 0);

        ValidationResult result = await validator.ValidateAsync(query);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task CreateWeightEntryCommandHandler_WithEmptyUserId_ReturnsInvalidToken() {
        var repository = new InMemoryWeightEntryRepository();
        var handler = new CreateWeightEntryCommandHandler(repository, CreateCurrentUserAccessService(User.Create("user@example.com", "hash")));

        Result<WeightEntryModel> result = await handler.Handle(
            new CreateWeightEntryCommand(Guid.Empty, DateTime.UtcNow, 82),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task CreateWeightEntryCommandHandler_NormalizesDateToUtcForDuplicateLookup() {
        var repository = new InMemoryWeightEntryRepository();
        var user = User.Create("weight@example.com", "hash");
        var handler = new CreateWeightEntryCommandHandler(repository, CreateCurrentUserAccessService(user));
        UserId userId = user.Id;
        var localDate = new DateTime(2026, 2, 23, 23, 30, 0, DateTimeKind.Local);
        DateTime expectedDate = NormalizeUtcDate(localDate);

        Result<WeightEntryModel> result = await handler.Handle(
            new CreateWeightEntryCommand(userId.Value, localDate, 82),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(expectedDate, repository.LastGetByDateDate);
        Assert.Equal(DateTimeKind.Utc, repository.LastGetByDateDate.Kind);
    }

    [Fact]
    public async Task CreateWeightEntryCommandHandler_WithDateOnlyValue_PreservesRequestedCalendarDate() {
        var repository = new InMemoryWeightEntryRepository();
        var user = User.Create("weight-dateonly@example.com", "hash");
        var handler = new CreateWeightEntryCommandHandler(repository, CreateCurrentUserAccessService(user));
        var dateOnly = new DateTime(2026, 5, 27, 0, 0, 0, DateTimeKind.Unspecified);
        var expectedDate = new DateTime(2026, 5, 27, 0, 0, 0, DateTimeKind.Utc);

        Result<WeightEntryModel> result = await handler.Handle(
            new CreateWeightEntryCommand(user.Id.Value, dateOnly, 82),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(expectedDate, repository.LastGetByDateDate);
        Assert.Equal(expectedDate, repository.AddedEntry?.Date);
    }

    [Fact]
    public async Task GetWeightSummariesQueryHandler_WithDateFromAfterDateTo_ReturnsValidationError() {
        var handler = new GetWeightSummariesQueryHandler(
            new InMemoryWeightEntryRepository(),
            CreateCurrentUserAccessService(User.Create("user@example.com", "hash")));
        var query = new GetWeightSummariesQuery(Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow.AddDays(-1), 7);

        Result<IReadOnlyList<WeightEntrySummaryModel>> result = await handler.Handle(query, CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Fact]
    public async Task GetWeightSummariesQueryHandler_WithMissingUserId_ReturnsInvalidToken() {
        var handler = new GetWeightSummariesQueryHandler(
            new InMemoryWeightEntryRepository(),
            CreateCurrentUserAccessService(User.Create("weight-summary-missing-user@example.com", "hash")));

        Result<IReadOnlyList<WeightEntrySummaryModel>> result = await handler.Handle(
            new GetWeightSummariesQuery(UserId: null, DateTime.UtcNow.AddDays(-7), DateTime.UtcNow, 7),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetWeightSummariesQueryHandler_WithNonPositiveQuantization_ReturnsValidationError() {
        var handler = new GetWeightSummariesQueryHandler(
            new InMemoryWeightEntryRepository(),
            CreateCurrentUserAccessService(User.Create("weight-summary-invalid-step@example.com", "hash")));

        Result<IReadOnlyList<WeightEntrySummaryModel>> result = await handler.Handle(
            new GetWeightSummariesQuery(Guid.NewGuid(), DateTime.UtcNow.AddDays(-7), DateTime.UtcNow, 0),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Fact]
    public async Task GetWeightSummariesQueryHandler_WithDateOnlyValues_PreservesRequestedCalendarDates() {
        var repository = new InMemoryWeightEntryRepository();
        var user = User.Create("weight-summary@example.com", "hash");
        var handler = new GetWeightSummariesQueryHandler(repository, CreateCurrentUserAccessService(user));
        var from = new DateTime(2026, 5, 20, 0, 0, 0, DateTimeKind.Unspecified);
        var to = new DateTime(2026, 5, 21, 0, 0, 0, DateTimeKind.Unspecified);

        Result<IReadOnlyList<WeightEntrySummaryModel>> result = await handler.Handle(
            new GetWeightSummariesQuery(user.Id.Value, from, to, 1),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(new DateTime(2026, 5, 20, 0, 0, 0, DateTimeKind.Utc), repository.LastPeriodDateFrom);
        Assert.Equal(new DateTime(2026, 5, 21, 0, 0, 0, DateTimeKind.Utc), repository.LastPeriodDateTo);
    }

    [Fact]
    public async Task GetWeightSummariesQueryHandler_WhenFinalBucketIsShorter_ReturnsRoundedAverage() {
        var repository = new InMemoryWeightEntryRepository();
        var user = User.Create("weight-summary-average@example.com", "hash");
        await repository.AddAsync(WeightEntry.Create(user.Id, new DateTime(2026, 5, 20, 0, 0, 0, DateTimeKind.Utc), 80.23));
        await repository.AddAsync(WeightEntry.Create(user.Id, new DateTime(2026, 5, 21, 0, 0, 0, DateTimeKind.Utc), 81.78));
        var handler = new GetWeightSummariesQueryHandler(repository, CreateCurrentUserAccessService(user));

        Result<IReadOnlyList<WeightEntrySummaryModel>> result = await handler.Handle(
            new GetWeightSummariesQuery(
                user.Id.Value,
                new DateTime(2026, 5, 20, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 5, 21, 0, 0, 0, DateTimeKind.Utc),
                7),
            CancellationToken.None);

        ResultAssert.Success(result);
        WeightEntrySummaryModel summary = Assert.Single(result.Value);
        Assert.Equal(new DateTime(2026, 5, 20, 0, 0, 0, DateTimeKind.Utc), summary.StartDate);
        Assert.Equal(new DateTime(2026, 5, 21, 0, 0, 0, DateTimeKind.Utc), summary.EndDate);
        Assert.Equal(81, summary.AverageWeight);
    }

    [Fact]
    public async Task GetWeightSummariesQueryHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-weight-summary@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new GetWeightSummariesQueryHandler(
            new InMemoryWeightEntryRepository(),
            CreateCurrentUserAccessService(user));

        Result<IReadOnlyList<WeightEntrySummaryModel>> result = await handler.Handle(
            new GetWeightSummariesQuery(user.Id.Value, DateTime.UtcNow.AddDays(-7), DateTime.UtcNow, 1),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task GetWeightEntriesQueryHandler_WithEmptyUserId_ReturnsInvalidToken() {
        var handler = new GetWeightEntriesQueryHandler(
            new InMemoryWeightEntryRepository(),
            CreateCurrentUserAccessService(User.Create("user@example.com", "hash")));

        Result<IReadOnlyList<WeightEntryModel>> result = await handler.Handle(
            new GetWeightEntriesQuery(Guid.Empty, DateFrom: null, DateTo: null, 10, Descending: true),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetWeightEntriesQueryHandler_WithDateOnlyRange_PreservesRequestedCalendarDates() {
        var repository = new InMemoryWeightEntryRepository();
        var user = User.Create("weight-list-dateonly@example.com", "hash");
        var handler = new GetWeightEntriesQueryHandler(repository, CreateCurrentUserAccessService(user));
        var from = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Unspecified);
        var to = new DateTime(2026, 5, 31, 0, 0, 0, DateTimeKind.Unspecified);

        Result<IReadOnlyList<WeightEntryModel>> result = await handler.Handle(
            new GetWeightEntriesQuery(user.Id.Value, from, to, 10, Descending: true),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc), repository.LastEntriesDateFrom);
        Assert.Equal(new DateTime(2026, 5, 31, 0, 0, 0, DateTimeKind.Utc), repository.LastEntriesDateTo);
    }

    [Fact]
    public async Task GetWeightEntriesQueryHandler_WithEntries_ReturnsMappedEntries() {
        var repository = new InMemoryWeightEntryRepository();
        var user = User.Create("weight-list-mapped@example.com", "hash");
        WeightEntry older = await repository.AddAsync(WeightEntry.Create(
            user.Id,
            new DateTime(2026, 5, 20, 0, 0, 0, DateTimeKind.Utc),
            82));
        WeightEntry newer = await repository.AddAsync(WeightEntry.Create(
            user.Id,
            new DateTime(2026, 5, 21, 0, 0, 0, DateTimeKind.Utc),
            81));
        await repository.AddAsync(WeightEntry.Create(UserId.New(), newer.Date.AddDays(1), 79));
        var handler = new GetWeightEntriesQueryHandler(repository, CreateCurrentUserAccessService(user));

        Result<IReadOnlyList<WeightEntryModel>> result = await handler.Handle(
            new GetWeightEntriesQuery(user.Id.Value, DateFrom: null, DateTo: null, 10, Descending: true),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Collection(
            result.Value,
            entry => {
                Assert.Equal(newer.Id.Value, entry.Id);
                Assert.Equal(81, entry.Weight);
            },
            entry => {
                Assert.Equal(older.Id.Value, entry.Id);
                Assert.Equal(82, entry.Weight);
            });
    }

    [Fact]
    public async Task DeleteWeightEntryCommandHandler_WithEmptyWeightEntryId_ReturnsValidationFailure() {
        var handler = new DeleteWeightEntryCommandHandler(
            new InMemoryWeightEntryRepository(),
            CreateCurrentUserAccessService(User.Create("user@example.com", "hash")));

        Result result = await handler.Handle(
            new DeleteWeightEntryCommand(Guid.NewGuid(), Guid.Empty),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("WeightEntryId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteWeightEntryCommandHandler_WithMissingUserId_ReturnsInvalidToken() {
        var handler = new DeleteWeightEntryCommandHandler(
            new InMemoryWeightEntryRepository(),
            CreateCurrentUserAccessService(User.Create("delete-weight-missing-user@example.com", "hash")));

        Result result = await handler.Handle(
            new DeleteWeightEntryCommand(UserId: null, WeightEntryId.New().Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task DeleteWeightEntryCommandHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("delete-weight-deleted@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new DeleteWeightEntryCommandHandler(
            new InMemoryWeightEntryRepository(),
            CreateCurrentUserAccessService(user));

        Result result = await handler.Handle(
            new DeleteWeightEntryCommand(user.Id.Value, WeightEntryId.New().Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task DeleteWeightEntryCommandHandler_WhenEntryMissing_ReturnsNotFound() {
        var user = User.Create("delete-weight-missing-entry@example.com", "hash");
        var handler = new DeleteWeightEntryCommandHandler(
            new InMemoryWeightEntryRepository(),
            CreateCurrentUserAccessService(user));

        Result result = await handler.Handle(
            new DeleteWeightEntryCommand(user.Id.Value, WeightEntryId.New().Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("WeightEntry.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task DeleteWeightEntryCommandHandler_WhenEntryExists_DeletesEntry() {
        var user = User.Create("delete-weight-success@example.com", "hash");
        var repository = new InMemoryWeightEntryRepository();
        WeightEntry entry = await repository.AddAsync(WeightEntry.Create(user.Id, DateTime.UtcNow.Date, 82));
        var handler = new DeleteWeightEntryCommandHandler(repository, CreateCurrentUserAccessService(user));

        Result result = await handler.Handle(
            new DeleteWeightEntryCommand(user.Id.Value, entry.Id.Value),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Null(await repository.GetByIdAsync(entry.Id, user.Id));
    }

    [Fact]
    public async Task UpdateWeightEntryCommandHandler_WithDateOnlyValue_PreservesRequestedCalendarDate() {
        var user = User.Create("weight-update-dateonly@example.com", "hash");
        var entry = WeightEntry.Create(user.Id, new DateTime(2026, 5, 26, 0, 0, 0, DateTimeKind.Utc), 82);
        var repository = new InMemoryWeightEntryRepository();
        await repository.AddAsync(entry);
        var handler = new UpdateWeightEntryCommandHandler(repository, CreateCurrentUserAccessService(user));
        var dateOnly = new DateTime(2026, 5, 27, 0, 0, 0, DateTimeKind.Unspecified);
        var expectedDate = new DateTime(2026, 5, 27, 0, 0, 0, DateTimeKind.Utc);

        Result<WeightEntryModel> result = await handler.Handle(
            new UpdateWeightEntryCommand(user.Id.Value, entry.Id.Value, dateOnly, 81),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(expectedDate, repository.LastGetByDateDate);
        Assert.Equal(expectedDate, entry.Date);
    }

    [Fact]
    public async Task UpdateWeightEntryCommandHandler_WithMissingUserId_ReturnsInvalidToken() {
        var handler = new UpdateWeightEntryCommandHandler(
            new InMemoryWeightEntryRepository(),
            CreateCurrentUserAccessService(User.Create("weight-update-missing-user@example.com", "hash")));

        Result<WeightEntryModel> result = await handler.Handle(
            new UpdateWeightEntryCommand(UserId: null, WeightEntryId.New().Value, DateTime.UtcNow, 82),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task UpdateWeightEntryCommandHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("weight-update-deleted-user@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new UpdateWeightEntryCommandHandler(
            new InMemoryWeightEntryRepository(),
            CreateCurrentUserAccessService(user));

        Result<WeightEntryModel> result = await handler.Handle(
            new UpdateWeightEntryCommand(user.Id.Value, WeightEntryId.New().Value, DateTime.UtcNow, 82),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task UpdateWeightEntryCommandHandler_WhenEntryMissing_ReturnsNotFound() {
        var user = User.Create("weight-update-missing-entry@example.com", "hash");
        var handler = new UpdateWeightEntryCommandHandler(
            new InMemoryWeightEntryRepository(),
            CreateCurrentUserAccessService(user));

        Result<WeightEntryModel> result = await handler.Handle(
            new UpdateWeightEntryCommand(user.Id.Value, WeightEntryId.New().Value, DateTime.UtcNow, 82),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("WeightEntry.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task UpdateWeightEntryCommandHandler_WhenAnotherEntryExistsForDate_ReturnsAlreadyExists() {
        var user = User.Create("weight-update-duplicate@example.com", "hash");
        var repository = new InMemoryWeightEntryRepository();
        WeightEntry entry = await repository.AddAsync(WeightEntry.Create(
            user.Id,
            new DateTime(2026, 5, 26, 0, 0, 0, DateTimeKind.Utc),
            82));
        WeightEntry duplicate = await repository.AddAsync(WeightEntry.Create(
            user.Id,
            new DateTime(2026, 5, 27, 0, 0, 0, DateTimeKind.Utc),
            81));
        var handler = new UpdateWeightEntryCommandHandler(repository, CreateCurrentUserAccessService(user));

        Result<WeightEntryModel> result = await handler.Handle(
            new UpdateWeightEntryCommand(user.Id.Value, entry.Id.Value, duplicate.Date, 80),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("WeightEntry.AlreadyExists", result.Error.Code);
    }

    [Fact]
    public async Task UpdateWeightEntryCommandHandler_WithEmptyWeightEntryId_ReturnsValidationFailure() {
        var handler = new UpdateWeightEntryCommandHandler(
            new InMemoryWeightEntryRepository(),
            CreateCurrentUserAccessService(User.Create("user@example.com", "hash")));

        Result<WeightEntryModel> result = await handler.Handle(
            new UpdateWeightEntryCommand(Guid.NewGuid(), Guid.Empty, DateTime.UtcNow, 82),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("WeightEntryId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetWeightEntriesQueryHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-weight@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new GetWeightEntriesQueryHandler(
            new InMemoryWeightEntryRepository(),
            CreateCurrentUserAccessService(user));

        Result<IReadOnlyList<WeightEntryModel>> result = await handler.Handle(
            new GetWeightEntriesQuery(user.Id.Value, DateFrom: null, DateTo: null, 10, Descending: true),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task CreateWeightEntryCommandHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-create-weight@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var repository = new InMemoryWeightEntryRepository();
        var handler = new CreateWeightEntryCommandHandler(repository, CreateCurrentUserAccessService(user));

        Result<WeightEntryModel> result = await handler.Handle(
            new CreateWeightEntryCommand(user.Id.Value, DateTime.UtcNow, 82),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
        Assert.Null(repository.AddedEntry);
    }

    [Fact]
    public async Task GetLatestWeightEntryQueryHandler_WithInvalidUserId_ReturnsInvalidToken() {
        var handler = new GetLatestWeightEntryQueryHandler(
            new InMemoryWeightEntryRepository(),
            CreateCurrentUserAccessService(User.Create("latest-weight@example.com", "hash")));

        Result<WeightEntryModel?> result = await handler.Handle(new GetLatestWeightEntryQuery(Guid.Empty), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetLatestWeightEntryQueryHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-latest-weight@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new GetLatestWeightEntryQueryHandler(
            new InMemoryWeightEntryRepository(),
            CreateCurrentUserAccessService(user));

        Result<WeightEntryModel?> result = await handler.Handle(new GetLatestWeightEntryQuery(user.Id.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task GetLatestWeightEntryQueryHandler_ReturnsMostRecentEntry() {
        var user = User.Create("latest-weight-entry@example.com", "hash");
        var repository = new InMemoryWeightEntryRepository();
        await repository.AddAsync(WeightEntry.Create(user.Id, new DateTime(2026, 5, 25, 0, 0, 0, DateTimeKind.Utc), 82));
        WeightEntry latest = await repository.AddAsync(WeightEntry.Create(user.Id, new DateTime(2026, 5, 27, 0, 0, 0, DateTimeKind.Utc), 81));
        await repository.AddAsync(WeightEntry.Create(UserId.New(), new DateTime(2026, 5, 28, 0, 0, 0, DateTimeKind.Utc), 79));
        var handler = new GetLatestWeightEntryQueryHandler(repository, CreateCurrentUserAccessService(user));

        Result<WeightEntryModel?> result = await handler.Handle(new GetLatestWeightEntryQuery(user.Id.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.NotNull(result.Value);
        Assert.Equal(latest.Id.Value, result.Value.Id);
        Assert.Equal(81, result.Value.Weight);
    }

    private static DateTime NormalizeUtcDate(DateTime value) {
        DateTime utc = value.Kind switch {
            DateTimeKind.Utc => value,
            _ => value.ToUniversalTime(),
        };

        return DateTime.SpecifyKind(utc.Date, DateTimeKind.Utc);
    }

    [ExcludeFromCodeCoverage]
    private sealed class InMemoryWeightEntryRepository : IWeightEntryRepository, IWeightEntryReadService {
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

        public async Task<IReadOnlyList<WeightEntryReadModel>> GetEntryReadModelsAsync(
            UserId userId,
            DateTime? dateFrom,
            DateTime? dateTo,
            int? limit,
            bool descending,
            CancellationToken cancellationToken = default) {
            IReadOnlyList<WeightEntry> entries = await GetEntriesAsync(
                userId,
                dateFrom,
                dateTo,
                limit,
                descending,
                cancellationToken).ConfigureAwait(false);

            return [.. entries.Select(entry => new WeightEntryReadModel(entry.Id.Value, entry.UserId.Value, entry.Date, entry.Weight))];
        }

        public async Task<IReadOnlyList<WeightEntryReadModel>> GetByPeriodReadModelsAsync(
            UserId userId,
            DateTime dateFrom,
            DateTime dateTo,
            CancellationToken cancellationToken = default) {
            IReadOnlyList<WeightEntry> entries = await GetByPeriodAsync(userId, dateFrom, dateTo, cancellationToken).ConfigureAwait(false);
            return [.. entries.Select(entry => new WeightEntryReadModel(entry.Id.Value, entry.UserId.Value, entry.Date, entry.Weight))];
        }

        async Task<IReadOnlyList<WeightEntryModel>> IWeightEntryReadService.GetEntriesAsync(
            UserId userId,
            DateTime? dateFrom,
            DateTime? dateTo,
            int? limit,
            bool descending,
            CancellationToken cancellationToken) {
            IReadOnlyList<WeightEntry> entries = await GetEntriesAsync(
                userId,
                dateFrom,
                dateTo,
                limit,
                descending,
                cancellationToken).ConfigureAwait(false);

            return [.. entries.Select(entry => entry.ToModel())];
        }

        async Task<WeightEntryModel?> IWeightEntryReadService.GetLatestAsync(UserId userId, CancellationToken cancellationToken) {
            IReadOnlyList<WeightEntryModel> entries = await ((IWeightEntryReadService)this)
                .GetEntriesAsync(userId, dateFrom: null, dateTo: null, limit: 1, descending: true, cancellationToken)
                .ConfigureAwait(false);

            return entries.Count > 0 ? entries[0] : null;
        }

        async Task<IReadOnlyList<WeightEntrySummaryModel>> IWeightEntryReadService.GetSummariesAsync(
            UserId userId,
            DateTime dateFrom,
            DateTime dateTo,
            int quantizationDays,
            CancellationToken cancellationToken) {
            IReadOnlyList<WeightEntry> entries = await GetByPeriodAsync(userId, dateFrom, dateTo, cancellationToken).ConfigureAwait(false);
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

        private static WeightEntrySummaryModel BuildResponse(
            DateTime start,
            DateTime end,
            IReadOnlyList<WeightEntry> entries) {
            List<WeightEntry> bucketEntries = [.. entries.Where(entry => entry.Date >= start && entry.Date <= end)];

            if (bucketEntries.Count == 0) {
                return new WeightEntrySummaryModel(start, end, 0);
            }

            double avg = bucketEntries.Average(entry => entry.Weight);
            return new WeightEntrySummaryModel(start, end, Math.Round(avg, 2, MidpointRounding.ToEven));
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
