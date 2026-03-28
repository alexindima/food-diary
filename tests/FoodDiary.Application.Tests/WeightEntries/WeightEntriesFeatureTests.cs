using FoodDiary.Application.WeightEntries.Commands.CreateWeightEntry;
using FoodDiary.Application.WeightEntries.Commands.DeleteWeightEntry;
using FoodDiary.Application.WeightEntries.Commands.UpdateWeightEntry;
using FoodDiary.Application.WeightEntries.Common;
using FoodDiary.Application.WeightEntries.Queries.GetWeightEntries;
using FoodDiary.Application.WeightEntries.Queries.GetWeightSummaries;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.WeightEntries;

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
        var handler = new CreateWeightEntryCommandHandler(repository);

        var result = await handler.Handle(
            new CreateWeightEntryCommand(Guid.Empty, DateTime.UtcNow, 82),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task CreateWeightEntryCommandHandler_NormalizesDateToUtcForDuplicateLookup() {
        var repository = new InMemoryWeightEntryRepository();
        var handler = new CreateWeightEntryCommandHandler(repository);
        var userId = UserId.New();
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
    public async Task GetWeightSummariesQueryHandler_WithDateFromAfterDateTo_ReturnsValidationError() {
        var handler = new GetWeightSummariesQueryHandler(new InMemoryWeightEntryRepository());
        var query = new GetWeightSummariesQuery(Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow.AddDays(-1), 7);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Fact]
    public async Task GetWeightEntriesQueryHandler_WithEmptyUserId_ReturnsInvalidToken() {
        var handler = new GetWeightEntriesQueryHandler(new InMemoryWeightEntryRepository());

        var result = await handler.Handle(
            new GetWeightEntriesQuery(Guid.Empty, null, null, 10, true),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task DeleteWeightEntryCommandHandler_WithEmptyWeightEntryId_ReturnsValidationFailure() {
        var handler = new DeleteWeightEntryCommandHandler(new InMemoryWeightEntryRepository());

        var result = await handler.Handle(
            new DeleteWeightEntryCommand(Guid.NewGuid(), Guid.Empty),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("WeightEntryId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateWeightEntryCommandHandler_WithEmptyWeightEntryId_ReturnsValidationFailure() {
        var handler = new UpdateWeightEntryCommandHandler(new InMemoryWeightEntryRepository());

        var result = await handler.Handle(
            new UpdateWeightEntryCommand(Guid.NewGuid(), Guid.Empty, DateTime.UtcNow, 82),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("WeightEntryId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static DateTime NormalizeUtcDate(DateTime value) {
        var utc = value.Kind switch {
            DateTimeKind.Utc => value,
            _ => value.ToUniversalTime()
        };

        return DateTime.SpecifyKind(utc.Date, DateTimeKind.Utc);
    }

    private sealed class InMemoryWeightEntryRepository : IWeightEntryRepository {
        private readonly List<WeightEntry> _entries = [];

        public DateTime LastGetByDateDate { get; private set; }

        public Task<WeightEntry> AddAsync(WeightEntry entry, CancellationToken cancellationToken = default) {
            _entries.Add(entry);
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
            var items = _entries
                .Where(x => x.UserId == userId && x.Date >= dateFrom && x.Date <= dateTo)
                .OrderBy(x => x.Date)
                .ToList();
            return Task.FromResult<IReadOnlyList<WeightEntry>>(items);
        }
    }
}
