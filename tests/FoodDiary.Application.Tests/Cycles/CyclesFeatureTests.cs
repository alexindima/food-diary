using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Cycles.Common;
using FoodDiary.Application.Cycles.Commands.CreateCycle;
using FoodDiary.Application.Cycles.Commands.UpsertCycleFactor;
using FoodDiary.Application.Cycles.Commands.UpsertCycleDay;
using FoodDiary.Application.Cycles.Mappings;
using FoodDiary.Application.Cycles.Models;
using FoodDiary.Application.Cycles.Queries.GetCurrentCycle;
using FoodDiary.Application.Cycles.Services;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Cycles;

[ExcludeFromCodeCoverage]
public class CyclesFeatureTests {
    [Fact]
    public async Task CreateCycleCommandValidator_WithInvalidLength_Fails() {
        var validator = new CreateCycleCommandValidator();
        var command = new CreateCycleCommand(
            Guid.NewGuid(),
            DateTime.UtcNow,
            (int)CycleTrackingMode.PeriodTracking,
            AverageCycleLength: 10,
            AveragePeriodLength: 20,
            LutealLength: 20,
            IsRegular: false,
            IsOnboardingComplete: false,
            ShowFertilityEstimates: false,
            DiscreetNotifications: true,
            Notes: null);

        FluentValidation.Results.ValidationResult result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task UpsertCycleDayCommandValidator_WithOutOfRangeSymptoms_Fails() {
        var validator = new UpsertCycleDayCommandValidator();
        var command = new UpsertCycleDayCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow,
            Bleeding: null,
            Symptoms: [new SymptomLogCommandModel((int)CycleSymptomCategory.Pain, 11, [], Note: null, ClearNote: false)],
            FertilitySignal: null);

        FluentValidation.Results.ValidationResult result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task UpsertCycleDayCommandHandler_WithEmptyCycleId_ReturnsValidationFailure() {
        var handler = new UpsertCycleDayCommandHandler(
            new NoopCycleRepository(),
            new StubUserRepository(User.Create("cycle-empty@example.com", "hash")));

        Result<CycleLogDayModel> result = await handler.Handle(
            new UpsertCycleDayCommand(
                Guid.NewGuid(),
                Guid.Empty,
                DateTime.UtcNow,
                Bleeding: null,
                Symptoms: [],
                FertilitySignal: null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("CycleProfileId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateCycleCommandHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-cycle@example.com", "hash");
        user.MarkDeleted(DateTime.UtcNow);
        var handler = new CreateCycleCommandHandler(new NoopCycleRepository(), new StubUserRepository(user));

        Result<CycleModel> result = await handler.Handle(CreateCommand(user.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task GetCurrentCycleQueryHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("cycle-current-deleted@example.com", "hash");
        user.MarkDeleted(DateTime.UtcNow);
        var handler = new GetCurrentCycleQueryHandler(new NoopCycleRepository(), new StubUserRepository(user));

        Result<CycleModel?> result = await handler.Handle(new GetCurrentCycleQuery(user.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task UpsertCycleDayCommandHandler_WhenProfileMissing_ReturnsNotFound() {
        var user = User.Create("cycle-day-missing@example.com", "hash");
        var handler = new UpsertCycleDayCommandHandler(new NoopCycleRepository(), new StubUserRepository(user));

        Result<CycleLogDayModel> result = await handler.Handle(
            new UpsertCycleDayCommand(
                user.Id.Value,
                Guid.NewGuid(),
                DateTime.UtcNow,
                new BleedingLogCommandModel((int)BleedingType.Bleeding, (int)CycleFlowLevel.Medium, PainImpact: 2, Notes: null, ClearNotes: false),
                Symptoms: [],
                FertilitySignal: null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Cycle.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task UpsertCycleDayCommandHandler_WithValidCommand_UpdatesProfileAndReturnsDay() {
        var user = User.Create("cycle-day-success@example.com", "hash");
        var profile = CycleProfile.Create(user.Id, new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc));
        var repository = new InMemoryCycleRepository(profile);
        var handler = new UpsertCycleDayCommandHandler(repository, new StubUserRepository(user));
        DateTime date = new(2026, 4, 2, 0, 0, 0, DateTimeKind.Utc);

        Result<CycleLogDayModel> result = await handler.Handle(
            new UpsertCycleDayCommand(
                user.Id.Value,
                profile.Id.Value,
                date,
                new BleedingLogCommandModel((int)BleedingType.Bleeding, (int)CycleFlowLevel.Medium, PainImpact: 3, Notes: "note", ClearNotes: false),
                [new SymptomLogCommandModel((int)CycleSymptomCategory.Craving, 7, ["sweet"], Note: null, ClearNote: false)],
                FertilitySignal: null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(profile.Id.Value, result.Value.CycleProfileId);
        Assert.Single(result.Value.BleedingEntries);
        Assert.Single(result.Value.Symptoms);
        Assert.True(repository.WasUpdated);
    }

    [Fact]
    public async Task UpsertCycleFactorCommandHandler_WithInvalidType_ReturnsValidationFailure() {
        var user = User.Create("cycle-factor-invalid@example.com", "hash");
        var profile = CycleProfile.Create(user.Id, new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc));
        var handler = new UpsertCycleFactorCommandHandler(new InMemoryCycleRepository(profile), new StubUserRepository(user));

        Result<CycleModel> result = await handler.Handle(
            new UpsertCycleFactorCommand(
                user.Id.Value,
                profile.Id.Value,
                Type: 999,
                StartDate: DateTime.UtcNow,
                EndDate: null,
                Notes: null,
                ClearNotes: false),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Fact]
    public async Task UpsertCycleFactorCommandHandler_WithValidCommand_UpdatesProfileAndReturnsCycle() {
        var user = User.Create("cycle-factor-success@example.com", "hash");
        var profile = CycleProfile.Create(user.Id, new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc));
        var repository = new InMemoryCycleRepository(profile);
        var handler = new UpsertCycleFactorCommandHandler(repository, new StubUserRepository(user));

        Result<CycleModel> result = await handler.Handle(
            new UpsertCycleFactorCommand(
                user.Id.Value,
                profile.Id.Value,
                (int)CycleFactorType.HormonalContraception,
                new DateTime(2026, 4, 2, 0, 0, 0, DateTimeKind.Utc),
                EndDate: null,
                Notes: "pill",
                ClearNotes: false),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Factors);
        Assert.Equal(CycleFactorType.HormonalContraception, result.Value.Factors.Single().Type);
        Assert.True(repository.WasUpdated);
    }

    [Fact]
    public void CycleMappings_ToModel_SortsLogsByDate() {
        var profile = CycleProfile.Create(UserId.New(), DateTime.UtcNow);
        profile.UpsertBleedingEntry(new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc), BleedingType.Bleeding, CycleFlowLevel.Light, painImpact: null, notes: null);
        profile.UpsertBleedingEntry(new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc), BleedingType.Bleeding, CycleFlowLevel.Heavy, painImpact: null, notes: null);

        CycleModel response = profile.ToModel();

        Assert.Equal(
            [new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc)],
            response.BleedingEntries.Select(day => day.Date));
    }

    [Fact]
    public void CyclePredictionService_CalculatePredictions_ReturnsRangeAndConfidence() {
        var profile = CycleProfile.Create(UserId.New(), new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc), showFertilityEstimates: true);

        CyclePredictionsModel predictions = CyclePredictionService.CalculatePredictions(profile);

        Assert.NotNull(predictions.NextPeriodStartFrom);
        Assert.NotNull(predictions.NextPeriodStartTo);
        Assert.NotNull(predictions.OvulationFrom);
        Assert.Equal("Learning", predictions.Confidence);
    }

    private static CreateCycleCommand CreateCommand(Guid userId) =>
        new(
            userId,
            new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            (int)CycleTrackingMode.PeriodTracking,
            AverageCycleLength: 28,
            AveragePeriodLength: 5,
            LutealLength: 14,
            IsRegular: false,
            IsOnboardingComplete: false,
            ShowFertilityEstimates: false,
            DiscreetNotifications: true,
            Notes: null);

    [ExcludeFromCodeCoverage]
    private sealed class NoopCycleRepository : ICycleRepository {
        public Task<CycleProfile> AddAsync(CycleProfile profile, CancellationToken cancellationToken = default) => Task.FromResult(profile);

        public Task UpdateAsync(CycleProfile profile, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<CycleProfile?> GetByIdAsync(CycleProfileId id, UserId userId, bool includeDetails = false, bool asTracking = false, CancellationToken cancellationToken = default) => Task.FromResult<CycleProfile?>(null);

        public Task<CycleProfile?> GetCurrentAsync(UserId userId, bool includeDetails = false, CancellationToken cancellationToken = default) => Task.FromResult<CycleProfile?>(null);

        public Task<IReadOnlyList<CycleProfile>> GetByUserAsync(UserId userId, bool includeDetails = false, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<CycleProfile>>([]);
    }

    [ExcludeFromCodeCoverage]
    private sealed class InMemoryCycleRepository(CycleProfile profile) : ICycleRepository {
        public bool WasUpdated { get; private set; }

        public Task<CycleProfile> AddAsync(CycleProfile profile, CancellationToken cancellationToken = default) => Task.FromResult(profile);

        public Task UpdateAsync(CycleProfile profile, CancellationToken cancellationToken = default) {
            WasUpdated = true;
            return Task.CompletedTask;
        }

        public Task<CycleProfile?> GetByIdAsync(CycleProfileId id, UserId userId, bool includeDetails = false, bool asTracking = false, CancellationToken cancellationToken = default) =>
            Task.FromResult(profile.Id == id && profile.UserId == userId ? profile : null);

        public Task<CycleProfile?> GetCurrentAsync(UserId userId, bool includeDetails = false, CancellationToken cancellationToken = default) =>
            Task.FromResult(profile.UserId == userId ? profile : null);

        public Task<IReadOnlyList<CycleProfile>> GetByUserAsync(UserId userId, bool includeDetails = false, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CycleProfile>>(profile.UserId == userId ? [profile] : []);
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubUserRepository(User? user) : IUserRepository {
        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) => Task.FromResult(user);
        public Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken cancellationToken = default) => Task.FromResult(user);
        public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default) => Task.FromResult(user is not null && user.Id == id ? user : null);
        public Task<User?> GetByIdIncludingDeletedAsync(UserId id, CancellationToken cancellationToken = default) => Task.FromResult(user is not null && user.Id == id ? user : null);
        public Task<User?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken = default) => Task.FromResult<User?>(null);
        public Task<User?> GetByTelegramUserIdIncludingDeletedAsync(long telegramUserId, CancellationToken cancellationToken = default) => Task.FromResult<User?>(null);
        public Task<(IReadOnlyList<User> Items, int TotalItems)> GetPagedAsync(string? search, int page, int limit, bool includeDeleted, CancellationToken cancellationToken = default) => Task.FromResult<(IReadOnlyList<User>, int)>((user is null ? [] : [user], user is null ? 0 : 1));
        public Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<User> RecentUsers)> GetAdminDashboardSummaryAsync(int recentLimit, CancellationToken cancellationToken = default) => Task.FromResult((user is null ? 0 : 1, user is { IsActive: true } ? 1 : 0, 0, user?.DeletedAt is null ? 0 : 1, (IReadOnlyList<User>)(user is null ? [] : [user])));
        public Task<IReadOnlyList<Role>> GetRolesByNamesAsync(IReadOnlyList<string> names, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Role>>([]);
        public Task<User> AddAsync(User user, CancellationToken cancellationToken = default) => Task.FromResult(user);
        public Task UpdateAsync(User user, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default) => Task.FromResult(user is not null);
    }
}
