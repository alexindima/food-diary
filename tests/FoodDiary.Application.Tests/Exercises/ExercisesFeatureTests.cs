using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Exercises.Commands.CreateExerciseEntry;
using FoodDiary.Application.Exercises.Commands.DeleteExerciseEntry;
using FoodDiary.Application.Exercises.Commands.UpdateExerciseEntry;
using FoodDiary.Application.Abstractions.Exercises.Common;
using FoodDiary.Application.Abstractions.Exercises.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Exercises.Common;
using FoodDiary.Application.Exercises.Mappings;
using FoodDiary.Application.Exercises.Queries.GetExerciseEntries;
using FoodDiary.Application.Exercises.Services;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;
using FoodDiary.Application.Exercises.Models;

namespace FoodDiary.Application.Tests.Exercises;

[ExcludeFromCodeCoverage]
public class ExercisesFeatureTests {
    [Fact]
    public async Task CreateExerciseEntry_WithValidData_Succeeds() {
        var repo = new InMemoryExerciseEntryRepository();
        var handler = new CreateExerciseEntryCommandHandler(repo, CreateCurrentUserAccessService());

        Result<ExerciseEntryModel> result = await handler.Handle(
            new CreateExerciseEntryCommand(Guid.NewGuid(), DateTime.UtcNow, "Running", 30, 250, "Jog", Notes: null),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal("Running", result.Value.ExerciseType);
        Assert.Equal(30, result.Value.DurationMinutes);
        Assert.Equal("Jog", result.Value.Name);
    }

    [Fact]
    public async Task CreateExerciseEntry_WithInvalidExerciseType_DefaultsToOther() {
        var repo = new InMemoryExerciseEntryRepository();
        var handler = new CreateExerciseEntryCommandHandler(repo, CreateCurrentUserAccessService());

        Result<ExerciseEntryModel> result = await handler.Handle(
            new CreateExerciseEntryCommand(Guid.NewGuid(), DateTime.UtcNow, "UnknownType", 30, 100, Name: null, Notes: null),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal("Other", result.Value.ExerciseType);
    }

    [Fact]
    public async Task CreateExerciseEntry_WithNullUserId_ReturnsFailure() {
        var handler = new CreateExerciseEntryCommandHandler(new InMemoryExerciseEntryRepository(), CreateCurrentUserAccessService());

        Result<ExerciseEntryModel> result = await handler.Handle(
            new CreateExerciseEntryCommand(UserId: null, DateTime.UtcNow, "Running", 30, 100, Name: null, Notes: null),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task CreateExerciseEntry_WhenUserCannotAccess_ReturnsInvalidToken() {
        var handler = new CreateExerciseEntryCommandHandler(
            new InMemoryExerciseEntryRepository(),
            CreateCurrentUserAccessService(Errors.Authentication.InvalidToken));

        Result<ExerciseEntryModel> result = await handler.Handle(
            new CreateExerciseEntryCommand(Guid.NewGuid(), DateTime.UtcNow, "Running", 30, 100, Name: null, Notes: null),
            CancellationToken.None);

        ResultAssert.Failure(result, "Authentication.InvalidToken");
    }

    [Fact]
    public async Task DeleteExerciseEntry_WhenExists_Succeeds() {
        var userId = UserId.New();
        var entry = ExerciseEntry.Create(userId, DateTime.UtcNow, ExerciseType.Running, 30, 200);
        var repo = new InMemoryExerciseEntryRepository();
        repo.Seed(entry);

        var handler = new DeleteExerciseEntryCommandHandler(repo, CreateCurrentUserAccessService());
        Result result = await handler.Handle(
            new DeleteExerciseEntryCommand(userId.Value, entry.Id.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.True(repo.DeleteCalled);
    }

    [Fact]
    public async Task DeleteExerciseEntry_WhenNotFound_ReturnsFailure() {
        var handler = new DeleteExerciseEntryCommandHandler(new InMemoryExerciseEntryRepository(), CreateCurrentUserAccessService());

        Result result = await handler.Handle(
            new DeleteExerciseEntryCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Exercise.NotAccessible", result.Error.Code);
    }

    [Fact]
    public async Task DeleteExerciseEntry_WithNullUserId_ReturnsFailure() {
        var handler = new DeleteExerciseEntryCommandHandler(new InMemoryExerciseEntryRepository(), CreateCurrentUserAccessService());

        Result result = await handler.Handle(
            new DeleteExerciseEntryCommand(UserId: null, Guid.NewGuid()), CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task DeleteExerciseEntry_WithEmptyEntryId_ReturnsValidationFailure() {
        var handler = new DeleteExerciseEntryCommandHandler(new InMemoryExerciseEntryRepository(), CreateCurrentUserAccessService());

        Result result = await handler.Handle(
            new DeleteExerciseEntryCommand(Guid.NewGuid(), Guid.Empty),
            CancellationToken.None);

        ResultAssert.Failure(result, "Validation.Invalid");
        Assert.Contains("EntryId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteExerciseEntry_WhenUserCannotAccess_ReturnsInvalidToken() {
        var handler = new DeleteExerciseEntryCommandHandler(
            new InMemoryExerciseEntryRepository(),
            CreateCurrentUserAccessService(Errors.Authentication.InvalidToken));

        Result result = await handler.Handle(
            new DeleteExerciseEntryCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        ResultAssert.Failure(result, "Authentication.InvalidToken");
    }

    [Fact]
    public async Task UpdateExerciseEntry_WhenExists_Succeeds() {
        var userId = UserId.New();
        var entry = ExerciseEntry.Create(userId, DateTime.UtcNow, ExerciseType.Running, 30, 200);
        var repo = new InMemoryExerciseEntryRepository();
        repo.Seed(entry);

        var handler = new UpdateExerciseEntryCommandHandler(repo, CreateCurrentUserAccessService());
        Result<ExerciseEntryModel> result = await handler.Handle(
            new UpdateExerciseEntryCommand(userId.Value, entry.Id.Value, "Swimming", 45, CaloriesBurned: null, Name: null, ClearName: false, Notes: null, ClearNotes: false, Date: null),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(45, result.Value.DurationMinutes);
    }

    [Fact]
    public async Task UpdateExerciseEntry_WhenNotFound_ReturnsFailure() {
        var handler = new UpdateExerciseEntryCommandHandler(new InMemoryExerciseEntryRepository(), CreateCurrentUserAccessService());

        Result<ExerciseEntryModel> result = await handler.Handle(
            new UpdateExerciseEntryCommand(Guid.NewGuid(), Guid.NewGuid(), ExerciseType: null, DurationMinutes: null, CaloriesBurned: null, Name: null, ClearName: false, Notes: null, ClearNotes: false, Date: null),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Exercise.NotAccessible", result.Error.Code);
    }

    [Fact]
    public async Task UpdateExerciseEntry_WithNullUserId_ReturnsFailure() {
        var handler = new UpdateExerciseEntryCommandHandler(new InMemoryExerciseEntryRepository(), CreateCurrentUserAccessService());

        Result<ExerciseEntryModel> result = await handler.Handle(
            new UpdateExerciseEntryCommand(UserId: null, Guid.NewGuid(), ExerciseType: null, DurationMinutes: null, CaloriesBurned: null, Name: null, ClearName: false, Notes: null, ClearNotes: false, Date: null),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task UpdateExerciseEntry_WithEmptyEntryId_ReturnsValidationFailure() {
        var handler = new UpdateExerciseEntryCommandHandler(new InMemoryExerciseEntryRepository(), CreateCurrentUserAccessService());

        Result<ExerciseEntryModel> result = await handler.Handle(
            new UpdateExerciseEntryCommand(Guid.NewGuid(), Guid.Empty, ExerciseType: null, DurationMinutes: null, CaloriesBurned: null, Name: null, ClearName: false, Notes: null, ClearNotes: false, Date: null),
            CancellationToken.None);

        ResultAssert.Failure(result, "Validation.Invalid");
        Assert.Contains("EntryId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateExerciseEntry_WhenUserCannotAccess_ReturnsInvalidToken() {
        var handler = new UpdateExerciseEntryCommandHandler(
            new InMemoryExerciseEntryRepository(),
            CreateCurrentUserAccessService(Errors.Authentication.InvalidToken));

        Result<ExerciseEntryModel> result = await handler.Handle(
            new UpdateExerciseEntryCommand(Guid.NewGuid(), Guid.NewGuid(), ExerciseType: null, DurationMinutes: null, CaloriesBurned: null, Name: null, ClearName: false, Notes: null, ClearNotes: false, Date: null),
            CancellationToken.None);

        ResultAssert.Failure(result, "Authentication.InvalidToken");
    }

    [Fact]
    public async Task GetExerciseEntries_ReturnsModels() {
        var userId = UserId.New();
        var entry = ExerciseEntry.Create(userId, DateTime.UtcNow, ExerciseType.Running, 30, 200);
        var repo = new InMemoryExerciseEntryRepository();
        repo.Seed(entry);

        var handler = new GetExerciseEntriesQueryHandler(repo, Substitute.For<ICurrentUserAccessService>());
        Result<IReadOnlyList<ExerciseEntryModel>> result = await handler.Handle(
            new GetExerciseEntriesQuery(userId.Value, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1)),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Single(result.Value);
    }

    [Fact]
    public async Task ExerciseEntryReadService_MapsReadModels() {
        var userId = UserId.New();
        var entry = ExerciseEntry.Create(userId, new DateTime(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc), ExerciseType.Cycling, 45, 320, "Bike", "Outdoor");
        var repo = new InMemoryExerciseEntryRepository();
        repo.Seed(entry);
        var service = new ExerciseEntryReadService(repo, repo);

        IReadOnlyList<ExerciseEntryModel> result = await service.GetEntriesAsync(
            userId,
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddDays(1),
            CancellationToken.None);

        ExerciseEntryModel model = Assert.Single(result);
        Assert.Multiple(
            () => Assert.Equal(entry.Id.Value, model.Id),
            () => Assert.Equal("Cycling", model.ExerciseType),
            () => Assert.Equal("Bike", model.Name),
            () => Assert.Equal(45, model.DurationMinutes),
            () => Assert.Equal(320, model.CaloriesBurned),
            () => Assert.Equal("Outdoor", model.Notes));
    }

    [Fact]
    public async Task GetExerciseEntries_WithNullUserId_ReturnsFailure() {
        var handler = new GetExerciseEntriesQueryHandler(new InMemoryExerciseEntryRepository(), Substitute.For<ICurrentUserAccessService>());

        Result<IReadOnlyList<ExerciseEntryModel>> result = await handler.Handle(
            new GetExerciseEntriesQuery(UserId: null, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [ExcludeFromCodeCoverage]
    private sealed class InMemoryExerciseEntryRepository : IExerciseEntryRepository, IExerciseEntryReadService {
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

        public Task<IReadOnlyList<ExerciseEntryReadModel>> GetByDateRangeReadModelsAsync(
            UserId userId,
            DateTime dateFrom,
            DateTime dateTo,
            CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ExerciseEntryReadModel>>([.. _entries
                .Where(entry => entry.UserId == userId)
                .Select(entry => new ExerciseEntryReadModel(
                    entry.Id.Value,
                    entry.Date,
                    entry.ExerciseType.ToString(),
                    entry.Name,
                    entry.DurationMinutes,
                    entry.CaloriesBurned,
                    entry.Notes))]);

        public Task<double> GetTotalCaloriesBurnedAsync(UserId userId, DateTime date, CancellationToken ct = default) => Task.FromResult(0.0);

        async Task<IReadOnlyList<ExerciseEntryModel>> IExerciseEntryReadService.GetEntriesAsync(
            UserId userId,
            DateTime dateFrom,
            DateTime dateTo,
            CancellationToken cancellationToken) {
            IReadOnlyList<ExerciseEntry> entries = await GetByDateRangeAsync(userId, dateFrom, dateTo, cancellationToken).ConfigureAwait(false);
            return [.. entries.Select(entry => entry.ToModel())];
        }
    }

    private static ICurrentUserAccessService CreateCurrentUserAccessService(Error? accessError = null) =>
        new StubCurrentUserAccessService(accessError);

    [ExcludeFromCodeCoverage]
    private sealed class StubCurrentUserAccessService(Error? accessError) : ICurrentUserAccessService {
        public Task<Error?> EnsureCanAccessAsync(UserId userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(accessError);
    }
}
