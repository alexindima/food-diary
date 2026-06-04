using FoodDiary.Application.Cycles.Commands.CreateCycle;
using FoodDiary.Application.Cycles.Commands.UpsertCycleDay;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Cycles.Common;
using FoodDiary.Application.Cycles.Mappings;
using FoodDiary.Application.Cycles.Models;
using FoodDiary.Application.Cycles.Queries.GetCurrentCycle;
using FoodDiary.Application.Cycles.Services;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Cycles;

[ExcludeFromCodeCoverage]
public class CyclesFeatureTests {
    [Fact]
    public async Task CreateCycleCommandValidator_WithInvalidLength_Fails() {
        var validator = new CreateCycleCommandValidator();
        var command = new CreateCycleCommand(Guid.NewGuid(), DateTime.UtcNow, AverageLength: 10, LutealLength: 20, Notes: null);

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task UpsertCycleDayCommandValidator_WithOutOfRangeSymptoms_Fails() {
        var validator = new UpsertCycleDayCommandValidator();
        var command = new UpsertCycleDayCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow,
            IsPeriod: true,
            Symptoms: new DailySymptomsModel(10, 0, 0, 0, 0, 0, 0),
            Notes: null,
            ClearNotes: false);

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task UpsertCycleDayCommandValidator_WithClearNotesAndValue_Fails() {
        var validator = new UpsertCycleDayCommandValidator();
        var command = new UpsertCycleDayCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow,
            IsPeriod: true,
            Symptoms: new DailySymptomsModel(1, 1, 1, 1, 1, 1, 1),
            Notes: "note",
            ClearNotes: true);

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => string.Equals(e.ErrorMessage, "Notes cannot be provided when ClearNotes is true.", StringComparison.Ordinal));
    }

    [Fact]
    public async Task UpsertCycleDayCommandHandler_WithEmptyCycleId_ReturnsValidationFailure() {
        var handler = new UpsertCycleDayCommandHandler(
            new NoopCycleRepository(),
            new StubUserRepository(User.Create("user@example.com", "hash")));

        var result = await handler.Handle(
            new UpsertCycleDayCommand(
                Guid.NewGuid(),
                Guid.Empty,
                DateTime.UtcNow,
                IsPeriod: true,
                Symptoms: new DailySymptomsModel(1, 1, 1, 1, 1, 1, 1),
                Notes: null,
                ClearNotes: false),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("CycleId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateCycleCommandHandler_WithEmptyUserId_ReturnsInvalidToken() {
        var handler = new CreateCycleCommandHandler(
            new NoopCycleRepository(),
            new StubUserRepository(User.Create("cycle-empty-user@example.com", "hash")));

        var result = await handler.Handle(
            new CreateCycleCommand(Guid.Empty, DateTime.UtcNow, 28, 14, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task CreateCycleCommandHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-cycle@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new CreateCycleCommandHandler(
            new NoopCycleRepository(),
            new StubUserRepository(user));

        var result = await handler.Handle(
            new CreateCycleCommand(user.Id.Value, DateTime.UtcNow, 28, 14, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task GetCurrentCycleQueryHandler_WithEmptyUserId_ReturnsInvalidToken() {
        var handler = new GetCurrentCycleQueryHandler(
            new NoopCycleRepository(),
            new StubUserRepository(User.Create("cycle-current-empty@example.com", "hash")));

        var result = await handler.Handle(new GetCurrentCycleQuery(Guid.Empty), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetCurrentCycleQueryHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("cycle-current-deleted@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new GetCurrentCycleQueryHandler(
            new NoopCycleRepository(),
            new StubUserRepository(user));

        var result = await handler.Handle(new GetCurrentCycleQuery(user.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task UpsertCycleDayCommandHandler_WithEmptyUserId_ReturnsInvalidToken() {
        var handler = new UpsertCycleDayCommandHandler(
            new NoopCycleRepository(),
            new StubUserRepository(User.Create("cycle-day-empty-user@example.com", "hash")));

        var result = await handler.Handle(
            new UpsertCycleDayCommand(
                Guid.Empty,
                Guid.NewGuid(),
                DateTime.UtcNow,
                IsPeriod: true,
                Symptoms: new DailySymptomsModel(1, 1, 1, 1, 1, 1, 1),
                Notes: null,
                ClearNotes: false),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task UpsertCycleDayCommandHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("cycle-day-deleted@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new UpsertCycleDayCommandHandler(
            new NoopCycleRepository(),
            new StubUserRepository(user));

        var result = await handler.Handle(
            new UpsertCycleDayCommand(
                user.Id.Value,
                Guid.NewGuid(),
                DateTime.UtcNow,
                IsPeriod: true,
                Symptoms: new DailySymptomsModel(1, 1, 1, 1, 1, 1, 1),
                Notes: null,
                ClearNotes: false),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task UpsertCycleDayCommandHandler_WhenCycleMissing_ReturnsNotFound() {
        var user = User.Create("cycle-day-missing@example.com", "hash");
        var cycleId = Guid.NewGuid();
        var handler = new UpsertCycleDayCommandHandler(
            new NoopCycleRepository(),
            new StubUserRepository(user));

        var result = await handler.Handle(
            new UpsertCycleDayCommand(
                user.Id.Value,
                cycleId,
                DateTime.UtcNow,
                IsPeriod: true,
                Symptoms: new DailySymptomsModel(1, 1, 1, 1, 1, 1, 1),
                Notes: null,
                ClearNotes: false),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Cycle.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task UpsertCycleDayCommandHandler_WithValidCommand_UpdatesCycleAndReturnsDay() {
        var user = User.Create("cycle-day-success@example.com", "hash");
        var cycle = Cycle.Create(user.Id, new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc));
        var repository = new InMemoryCycleRepository(cycle);
        var handler = new UpsertCycleDayCommandHandler(repository, new StubUserRepository(user));
        var date = new DateTime(2026, 4, 5, 12, 30, 0, DateTimeKind.Local);

        var result = await handler.Handle(
            new UpsertCycleDayCommand(
                user.Id.Value,
                cycle.Id.Value,
                date,
                IsPeriod: true,
                Symptoms: new DailySymptomsModel(1, 2, 3, 4, 5, 6, 7),
                Notes: "day note",
                ClearNotes: false),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(repository.UpdateCalled);
        Assert.Equal(cycle.Id.Value, result.Value.CycleId);
        Assert.True(result.Value.IsPeriod);
        Assert.Equal(7, result.Value.Symptoms.Libido);
        Assert.Equal("day note", result.Value.Notes);
    }

    [Fact]
    public void CycleMappings_ToModel_SortsDaysByDate() {
        var cycle = Cycle.Create(UserId.New(), DateTime.UtcNow);
        cycle.AddOrUpdateDay(new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc), true, DailySymptoms.Create(1, 1, 1, 1, 1, 1, 1));
        cycle.AddOrUpdateDay(new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc), false, DailySymptoms.Create(2, 2, 2, 2, 2, 2, 2));

        var response = cycle.ToModel();

        Assert.Collection(
            response.Days,
            day => Assert.Equal(new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc), day.Date),
            day => Assert.Equal(new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc), day.Date));
    }

    [Fact]
    public void CycleMappings_ToValueObject_MapsSymptomValues() {
        var model = new DailySymptomsModel(1, 2, 3, 4, 5, 6, 7);

        var symptoms = model.ToValueObject();

        Assert.Equal(1, symptoms.Pain);
        Assert.Equal(2, symptoms.Mood);
        Assert.Equal(3, symptoms.Edema);
        Assert.Equal(4, symptoms.Headache);
        Assert.Equal(5, symptoms.Energy);
        Assert.Equal(6, symptoms.SleepQuality);
        Assert.Equal(7, symptoms.Libido);
    }

    [Fact]
    public void CyclePredictionService_CalculatePredictions_NormalizesToUtcDate() {
        var localStart = DateTime.SpecifyKind(new DateTime(2026, 1, 10, 23, 30, 0), DateTimeKind.Local);
        var cycle = Cycle.Create(UserId.New(), localStart, averageLength: 28, lutealLength: 14);

        var predictions = CyclePredictionService.CalculatePredictions(cycle);

        Assert.NotNull(predictions.NextPeriodStart);
        Assert.NotNull(predictions.OvulationDate);
        Assert.NotNull(predictions.PmsStart);
        Assert.Equal(DateTimeKind.Utc, predictions.NextPeriodStart!.Value.Kind);
        Assert.Equal(DateTimeKind.Utc, predictions.OvulationDate!.Value.Kind);
        Assert.Equal(DateTimeKind.Utc, predictions.PmsStart!.Value.Kind);
    }

    [Fact]
    public void CyclePredictionService_CalculatePredictions_WithNullCycle_Throws() {
        Assert.Throws<ArgumentNullException>(() => CyclePredictionService.CalculatePredictions(null!));
    }

    [ExcludeFromCodeCoverage]
    private sealed class NoopCycleRepository : ICycleRepository {
        public Task<Cycle> AddAsync(Cycle cycle, CancellationToken cancellationToken = default) => Task.FromResult(cycle);
        public Task UpdateAsync(Cycle cycle, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<Cycle?> GetByIdAsync(CycleId id, UserId userId, bool includeDays = false, bool asTracking = false, CancellationToken cancellationToken = default) => Task.FromResult<Cycle?>(null);
        public Task<Cycle?> GetLatestAsync(UserId userId, bool includeDays = false, CancellationToken cancellationToken = default) => Task.FromResult<Cycle?>(null);
        public Task<IReadOnlyList<Cycle>> GetByUserAsync(UserId userId, bool includeDays = false, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Cycle>>([]);
    }

    [ExcludeFromCodeCoverage]
    private sealed class InMemoryCycleRepository(Cycle cycle) : ICycleRepository {
        public bool UpdateCalled { get; private set; }

        public Task<Cycle> AddAsync(Cycle cycle, CancellationToken cancellationToken = default) => Task.FromResult(cycle);

        public Task UpdateAsync(Cycle cycle, CancellationToken cancellationToken = default) {
            UpdateCalled = true;
            return Task.CompletedTask;
        }

        public Task<Cycle?> GetByIdAsync(CycleId id, UserId userId, bool includeDays = false, bool asTracking = false, CancellationToken cancellationToken = default) =>
            Task.FromResult(cycle.Id == id && cycle.UserId == userId ? cycle : null);

        public Task<Cycle?> GetLatestAsync(UserId userId, bool includeDays = false, CancellationToken cancellationToken = default) =>
            Task.FromResult(cycle.UserId == userId ? cycle : null);

        public Task<IReadOnlyList<Cycle>> GetByUserAsync(UserId userId, bool includeDays = false, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Cycle>>(cycle.UserId == userId ? [cycle] : []);
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
