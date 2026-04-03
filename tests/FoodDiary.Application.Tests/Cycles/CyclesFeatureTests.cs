using FoodDiary.Application.Cycles.Commands.CreateCycle;
using FoodDiary.Application.Cycles.Commands.UpsertCycleDay;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Cycles.Common;
using FoodDiary.Application.Cycles.Mappings;
using FoodDiary.Application.Cycles.Models;
using FoodDiary.Application.Cycles.Services;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Cycles;

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
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Notes cannot be provided when ClearNotes is true.");
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

    private sealed class NoopCycleRepository : ICycleRepository {
        public Task<Cycle> AddAsync(Cycle cycle, CancellationToken cancellationToken = default) => Task.FromResult(cycle);
        public Task UpdateAsync(Cycle cycle, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<Cycle?> GetByIdAsync(CycleId id, UserId userId, bool includeDays = false, bool asTracking = false, CancellationToken cancellationToken = default) => Task.FromResult<Cycle?>(null);
        public Task<Cycle?> GetLatestAsync(UserId userId, bool includeDays = false, CancellationToken cancellationToken = default) => Task.FromResult<Cycle?>(null);
        public Task<IReadOnlyList<Cycle>> GetByUserAsync(UserId userId, bool includeDays = false, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Cycle>>([]);
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
}
