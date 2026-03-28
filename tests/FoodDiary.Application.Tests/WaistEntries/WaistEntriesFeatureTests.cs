using FoodDiary.Application.WaistEntries.Commands.CreateWaistEntry;
using FoodDiary.Application.WaistEntries.Commands.DeleteWaistEntry;
using FoodDiary.Application.WaistEntries.Commands.UpdateWaistEntry;
using FoodDiary.Application.WaistEntries.Common;
using FoodDiary.Application.WaistEntries.Queries.GetWaistEntries;
using FoodDiary.Application.WaistEntries.Queries.GetWaistSummaries;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.WaistEntries;

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
        var handler = new CreateWaistEntryCommandHandler(repository);

        var result = await handler.Handle(
            new CreateWaistEntryCommand(Guid.Empty, DateTime.UtcNow, 82),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task CreateWaistEntryCommandHandler_NormalizesDateToUtcForDuplicateLookup() {
        var repository = new InMemoryWaistEntryRepository();
        var handler = new CreateWaistEntryCommandHandler(repository);
        var userId = UserId.New();
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
    public async Task GetWaistSummariesQueryHandler_WithDateFromAfterDateTo_ReturnsValidationError() {
        var handler = new GetWaistSummariesQueryHandler(new InMemoryWaistEntryRepository());
        var query = new GetWaistSummariesQuery(Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow.AddDays(-1), 7);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Fact]
    public async Task DeleteWaistEntryCommandHandler_WithEmptyWaistEntryId_ReturnsValidationFailure() {
        var handler = new DeleteWaistEntryCommandHandler(new InMemoryWaistEntryRepository());

        var result = await handler.Handle(
            new DeleteWaistEntryCommand(Guid.NewGuid(), Guid.Empty),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("WaistEntryId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateWaistEntryCommandHandler_WithEmptyWaistEntryId_ReturnsValidationFailure() {
        var handler = new UpdateWaistEntryCommandHandler(new InMemoryWaistEntryRepository());

        var result = await handler.Handle(
            new UpdateWaistEntryCommand(Guid.NewGuid(), Guid.Empty, DateTime.UtcNow, 82),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("WaistEntryId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static DateTime NormalizeUtcDate(DateTime value) {
        var utc = value.Kind switch {
            DateTimeKind.Utc => value,
            _ => value.ToUniversalTime()
        };

        return DateTime.SpecifyKind(utc.Date, DateTimeKind.Utc);
    }

    private sealed class InMemoryWaistEntryRepository : IWaistEntryRepository {
        private readonly List<WaistEntry> _entries = [];

        public DateTime LastGetByDateDate { get; private set; }

        public Task<WaistEntry> AddAsync(WaistEntry entry, CancellationToken cancellationToken = default) {
            _entries.Add(entry);
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
            var items = _entries
                .Where(x => x.UserId == userId && x.Date >= dateFrom && x.Date <= dateTo)
                .OrderBy(x => x.Date)
                .ToList();
            return Task.FromResult<IReadOnlyList<WaistEntry>>(items);
        }
    }
}
