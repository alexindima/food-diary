using FoodDiary.Application.Exercises.Commands.CreateExerciseEntry;
using FoodDiary.Application.Exercises.Commands.DeleteExerciseEntry;
using FoodDiary.Application.Exercises.Commands.UpdateExerciseEntry;
using FoodDiary.Application.Exercises.Common;
using FoodDiary.Application.Exercises.Queries.GetExerciseEntries;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Exercises;

public class ExercisesFeatureTests {
    [Fact]
    public async Task CreateExerciseEntry_WithValidData_Succeeds() {
        var repo = new InMemoryExerciseEntryRepository();
        var handler = new CreateExerciseEntryCommandHandler(repo);

        var result = await handler.Handle(
            new CreateExerciseEntryCommand(Guid.NewGuid(), DateTime.UtcNow, "Running", 30, 250, "Jog", null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Running", result.Value.ExerciseType);
        Assert.Equal(30, result.Value.DurationMinutes);
        Assert.Equal("Jog", result.Value.Name);
    }

    [Fact]
    public async Task CreateExerciseEntry_WithInvalidExerciseType_DefaultsToOther() {
        var repo = new InMemoryExerciseEntryRepository();
        var handler = new CreateExerciseEntryCommandHandler(repo);

        var result = await handler.Handle(
            new CreateExerciseEntryCommand(Guid.NewGuid(), DateTime.UtcNow, "UnknownType", 30, 100, null, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Other", result.Value.ExerciseType);
    }

    [Fact]
    public async Task CreateExerciseEntry_WithNullUserId_ReturnsFailure() {
        var handler = new CreateExerciseEntryCommandHandler(new InMemoryExerciseEntryRepository());

        var result = await handler.Handle(
            new CreateExerciseEntryCommand(null, DateTime.UtcNow, "Running", 30, 100, null, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task DeleteExerciseEntry_WhenExists_Succeeds() {
        var userId = UserId.New();
        var entry = ExerciseEntry.Create(userId, DateTime.UtcNow, ExerciseType.Running, 30, 200);
        var repo = new InMemoryExerciseEntryRepository();
        repo.Seed(entry);

        var handler = new DeleteExerciseEntryCommandHandler(repo);
        var result = await handler.Handle(
            new DeleteExerciseEntryCommand(userId.Value, entry.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(repo.DeleteCalled);
    }

    [Fact]
    public async Task DeleteExerciseEntry_WhenNotFound_ReturnsFailure() {
        var handler = new DeleteExerciseEntryCommandHandler(new InMemoryExerciseEntryRepository());

        var result = await handler.Handle(
            new DeleteExerciseEntryCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task UpdateExerciseEntry_WhenExists_Succeeds() {
        var userId = UserId.New();
        var entry = ExerciseEntry.Create(userId, DateTime.UtcNow, ExerciseType.Running, 30, 200);
        var repo = new InMemoryExerciseEntryRepository();
        repo.Seed(entry);

        var handler = new UpdateExerciseEntryCommandHandler(repo);
        var result = await handler.Handle(
            new UpdateExerciseEntryCommand(userId.Value, entry.Id.Value, "Swimming", 45, null, null, false, null, false, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(45, result.Value.DurationMinutes);
    }

    [Fact]
    public async Task UpdateExerciseEntry_WhenNotFound_ReturnsFailure() {
        var handler = new UpdateExerciseEntryCommandHandler(new InMemoryExerciseEntryRepository());

        var result = await handler.Handle(
            new UpdateExerciseEntryCommand(Guid.NewGuid(), Guid.NewGuid(), null, null, null, null, false, null, false, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task GetExerciseEntries_ReturnsModels() {
        var userId = UserId.New();
        var entry = ExerciseEntry.Create(userId, DateTime.UtcNow, ExerciseType.Running, 30, 200);
        var repo = new InMemoryExerciseEntryRepository();
        repo.Seed(entry);

        var handler = new GetExerciseEntriesQueryHandler(repo);
        var result = await handler.Handle(
            new GetExerciseEntriesQuery(userId.Value, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
    }

    private sealed class InMemoryExerciseEntryRepository : IExerciseEntryRepository {
        private readonly List<ExerciseEntry> _entries = [];
        public bool DeleteCalled { get; private set; }

        public void Seed(ExerciseEntry entry) => _entries.Add(entry);

        public Task<ExerciseEntry> AddAsync(ExerciseEntry entry, CancellationToken ct = default) {
            _entries.Add(entry);
            return Task.FromResult(entry);
        }

        public Task UpdateAsync(ExerciseEntry entry, CancellationToken ct = default) => Task.CompletedTask;

        public Task DeleteAsync(ExerciseEntry entry, CancellationToken ct = default) {
            DeleteCalled = true;
            _entries.Remove(entry);
            return Task.CompletedTask;
        }

        public Task<ExerciseEntry?> GetByIdAsync(ExerciseEntryId id, UserId userId, bool asTracking = false, CancellationToken ct = default) =>
            Task.FromResult(_entries.FirstOrDefault(e => e.Id == id && e.UserId == userId));

        public Task<IReadOnlyList<ExerciseEntry>> GetByDateRangeAsync(UserId userId, DateTime dateFrom, DateTime dateTo, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ExerciseEntry>>(_entries.Where(e => e.UserId == userId).ToList());

        public Task<double> GetTotalCaloriesBurnedAsync(UserId userId, DateTime date, CancellationToken ct = default) => Task.FromResult(0.0);
    }
}
