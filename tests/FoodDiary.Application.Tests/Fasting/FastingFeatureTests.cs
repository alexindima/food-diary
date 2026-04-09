using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Application.Common.Abstractions.Persistence;
using FoodDiary.Application.Fasting.Commands.EndFasting;
using FoodDiary.Application.Fasting.Commands.ExtendActiveFasting;
using FoodDiary.Application.Fasting.Commands.PostponeCyclicFastDay;
using FoodDiary.Application.Fasting.Commands.SkipCyclicFastDay;
using FoodDiary.Application.Fasting.Commands.StartFasting;
using FoodDiary.Application.Fasting.Common;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Fasting;

public class FastingFeatureTests {
    private static readonly DateTime FixedNow = new(2026, 4, 6, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task StartFasting_WithValidData_CreatesSession() {
        var user = User.Create("user@example.com", "hash");
        var planRepo = new InMemoryFastingPlanRepository();
        var occurrenceRepo = new InMemoryFastingOccurrenceRepository();
        var handler = new StartFastingCommandHandler(planRepo, occurrenceRepo, new StubUserRepository(user), new FixedDateTimeProvider(), new StubUnitOfWork());

        var result = await handler.Handle(
            new StartFastingCommand(user.Id.Value, "F16_8", null, null, null, null, null, null, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("F16_8", result.Value.Protocol);
        Assert.Equal(16, result.Value.InitialPlannedDurationHours);
        Assert.Equal(0, result.Value.AddedDurationHours);
        Assert.Equal(16, result.Value.PlannedDurationHours);
        Assert.False(result.Value.IsCompleted);
        Assert.Equal("Active", result.Value.Status);
    }

    [Fact]
    public async Task StartFasting_WithCustomIntermittent_CreatesSession() {
        var user = User.Create("user@example.com", "hash");
        var planRepo = new InMemoryFastingPlanRepository();
        var occurrenceRepo = new InMemoryFastingOccurrenceRepository();
        var handler = new StartFastingCommandHandler(planRepo, occurrenceRepo, new StubUserRepository(user), new FixedDateTimeProvider(), new StubUnitOfWork());

        var result = await handler.Handle(
            new StartFastingCommand(user.Id.Value, "CustomIntermittent", null, 17, null, null, null, null, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("CustomIntermittent", result.Value.Protocol);
        Assert.Equal(17, result.Value.InitialPlannedDurationHours);
        Assert.Equal(17, result.Value.PlannedDurationHours);
        Assert.Equal("Active", result.Value.Status);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(24)]
    [InlineData(30)]
    public async Task StartFasting_WithInvalidCustomIntermittentDuration_ReturnsFailure(int duration) {
        var user = User.Create("user@example.com", "hash");
        var planRepo = new InMemoryFastingPlanRepository();
        var occurrenceRepo = new InMemoryFastingOccurrenceRepository();
        var handler = new StartFastingCommandHandler(planRepo, occurrenceRepo, new StubUserRepository(user), new FixedDateTimeProvider(), new StubUnitOfWork());

        var result = await handler.Handle(
            new StartFastingCommand(user.Id.Value, "CustomIntermittent", null, duration, null, null, null, null, null), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task StartFasting_WhenAlreadyActive_ReturnsFailure() {
        var user = User.Create("user@example.com", "hash");
        var existingPlan = FastingPlan.CreateIntermittent(user.Id, FastingProtocol.F16_8, 16, 8, FixedNow);
        var planRepo = new InMemoryFastingPlanRepository(active: existingPlan);
        var occurrenceRepo = new InMemoryFastingOccurrenceRepository();
        var handler = new StartFastingCommandHandler(planRepo, occurrenceRepo, new StubUserRepository(user), new FixedDateTimeProvider(), new StubUnitOfWork());

        var result = await handler.Handle(
            new StartFastingCommand(user.Id.Value, "F18_6", null, null, null, null, null, null, null), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("AlreadyActive", result.Error.Code);
    }

    [Fact]
    public async Task StartFasting_WithInvalidProtocol_ReturnsFailure() {
        var user = User.Create("user@example.com", "hash");
        var handler = new StartFastingCommandHandler(
            new InMemoryFastingPlanRepository(), new InMemoryFastingOccurrenceRepository(), new StubUserRepository(user), new FixedDateTimeProvider(), new StubUnitOfWork());

        var result = await handler.Handle(
            new StartFastingCommand(user.Id.Value, "InvalidProtocol", null, null, null, null, null, null, null), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task StartFasting_WithNullUserId_ReturnsFailure() {
        var handler = new StartFastingCommandHandler(
            new InMemoryFastingPlanRepository(), new InMemoryFastingOccurrenceRepository(), new StubUserRepository(null), new FixedDateTimeProvider(), new StubUnitOfWork());

        var result = await handler.Handle(
            new StartFastingCommand(null, "F16_8", null, null, null, null, null, null, null), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task StartFasting_WithCyclicPlan_CreatesCyclicSession() {
        var user = User.Create("user@example.com", "hash");
        var planRepo = new InMemoryFastingPlanRepository();
        var occurrenceRepo = new InMemoryFastingOccurrenceRepository();
        var handler = new StartFastingCommandHandler(planRepo, occurrenceRepo, new StubUserRepository(user), new FixedDateTimeProvider(), new StubUnitOfWork());

        var result = await handler.Handle(
            new StartFastingCommand(user.Id.Value, null, "Cyclic", null, 1, 3, 16, 8, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Cyclic", result.Value.PlanType);
        Assert.Equal("FastDay", result.Value.OccurrenceKind);
        Assert.Equal(1, result.Value.CyclicFastDays);
        Assert.Equal(3, result.Value.CyclicEatDays);
    }

    [Fact]
    public async Task EndFasting_WhenActiveSession_Succeeds() {
        var userId = UserId.New();
        var plan = FastingPlan.CreateIntermittent(userId, FastingProtocol.F16_8, 16, 8, FixedNow);
        var occurrence = FastingOccurrence.Create(plan.Id, userId, FastingOccurrenceKind.FastingWindow, FixedNow, 1, 16);
        var handler = new EndFastingCommandHandler(
            new InMemoryFastingPlanRepository(active: plan),
            new InMemoryFastingOccurrenceRepository(current: occurrence),
            new FixedDateTimeProvider(),
            new StubUnitOfWork());

        var result = await handler.Handle(
            new EndFastingCommand(userId.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsCompleted);
        Assert.Equal("Completed", result.Value.Status);
    }

    [Fact]
    public async Task EndCyclicFastDay_AdvancesToEatDayInsteadOfStoppingPlan() {
        var userId = UserId.New();
        var plan = FastingPlan.CreateCyclic(userId, 1, 3, 16, 8, FixedNow, FixedNow);
        var occurrence = FastingOccurrence.Create(plan.Id, userId, FastingOccurrenceKind.FastDay, FixedNow, 1, 24);
        var occurrenceRepo = new InMemoryFastingOccurrenceRepository(occurrence);
        var handler = new EndFastingCommandHandler(
            new InMemoryFastingPlanRepository(active: plan),
            occurrenceRepo,
            new FixedDateTimeProvider(),
            new StubUnitOfWork());

        var result = await handler.Handle(
            new EndFastingCommand(userId.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("EatDay", result.Value.OccurrenceKind);
        Assert.Equal("Active", result.Value.Status);
        Assert.Null(result.Value.EndedAtUtc);
        Assert.Equal(FastingPlanStatus.Active, plan.Status);
        Assert.Contains(occurrenceRepo.StoredOccurrences, x => x.Kind == FastingOccurrenceKind.EatDay && x.Status == FastingOccurrenceStatus.Active);
    }

    [Fact]
    public async Task EndCyclicFastDay_InMultiDayFastBlock_CreatesAnotherFastDay() {
        var userId = UserId.New();
        var plan = FastingPlan.CreateCyclic(userId, 3, 1, 16, 8, FixedNow, FixedNow);
        var first = FastingOccurrence.Create(plan.Id, userId, FastingOccurrenceKind.FastDay, FixedNow.AddDays(-1), 1, 24);
        first.Complete(FixedNow.AddHours(-12));
        var current = FastingOccurrence.Create(plan.Id, userId, FastingOccurrenceKind.FastDay, FixedNow, 2, 24);
        var occurrenceRepo = new InMemoryFastingOccurrenceRepository(current);
        occurrenceRepo.StoredOccurrences.Insert(0, first);
        var handler = new EndFastingCommandHandler(
            new InMemoryFastingPlanRepository(active: plan),
            occurrenceRepo,
            new FixedDateTimeProvider(),
            new StubUnitOfWork());

        var result = await handler.Handle(
            new EndFastingCommand(userId.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("FastDay", result.Value.OccurrenceKind);
        Assert.Equal("Active", result.Value.Status);
    }

    [Fact]
    public async Task EndExtendedFasting_BeforeTarget_ReturnsInterruptedStatus() {
        var userId = UserId.New();
        var plan = FastingPlan.CreateExtended(userId, FastingProtocol.F72_0, 72, FixedNow);
        var occurrence = FastingOccurrence.Create(plan.Id, userId, FastingOccurrenceKind.FastDay, FixedNow, 1, 72);
        var handler = new EndFastingCommandHandler(
            new InMemoryFastingPlanRepository(active: plan),
            new InMemoryFastingOccurrenceRepository(current: occurrence),
            new FixedDateTimeProvider(),
            new StubUnitOfWork());

        var result = await handler.Handle(
            new EndFastingCommand(userId.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsCompleted);
        Assert.Equal("Interrupted", result.Value.Status);
    }

    [Fact]
    public async Task EndFasting_WhenNoActiveSession_ReturnsFailure() {
        var handler = new EndFastingCommandHandler(
            new InMemoryFastingPlanRepository(), new InMemoryFastingOccurrenceRepository(), new FixedDateTimeProvider(), new StubUnitOfWork());

        var result = await handler.Handle(
            new EndFastingCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("NoActiveSession", result.Error.Code);
    }

    [Fact]
    public async Task ExtendActiveFasting_WhenSessionIsActive_Succeeds() {
        var userId = UserId.New();
        var plan = FastingPlan.CreateExtended(userId, FastingProtocol.F72_0, 72, FixedNow);
        var occurrence = FastingOccurrence.Create(plan.Id, userId, FastingOccurrenceKind.FastDay, FixedNow, 1, 72);
        var handler = new ExtendActiveFastingCommandHandler(
            new InMemoryFastingPlanRepository(active: plan),
            new InMemoryFastingOccurrenceRepository(current: occurrence),
            new StubUnitOfWork());

        var result = await handler.Handle(
            new ExtendActiveFastingCommand(userId.Value, 24), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(72, result.Value.InitialPlannedDurationHours);
        Assert.Equal(24, result.Value.AddedDurationHours);
        Assert.Equal(96, result.Value.PlannedDurationHours);
    }

    [Fact]
    public async Task ExtendActiveFasting_WhenNoActiveSession_ReturnsFailure() {
        var handler = new ExtendActiveFastingCommandHandler(
            new InMemoryFastingPlanRepository(),
            new InMemoryFastingOccurrenceRepository(),
            new StubUnitOfWork());

        var result = await handler.Handle(
            new ExtendActiveFastingCommand(Guid.NewGuid(), 24), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("NoActiveSession", result.Error.Code);
    }

    [Fact]
    public async Task SkipCyclicFastDay_WhenActiveFastDay_CreatesEatDayOccurrence() {
        var userId = UserId.New();
        var plan = FastingPlan.CreateCyclic(userId, 1, 3, 16, 8, FixedNow, FixedNow);
        var occurrence = FastingOccurrence.Create(plan.Id, userId, FastingOccurrenceKind.FastDay, FixedNow, 1, 24);
        var occurrenceRepo = new InMemoryFastingOccurrenceRepository(current: occurrence);
        var handler = new SkipCyclicFastDayCommandHandler(
            new InMemoryFastingPlanRepository(active: plan),
            occurrenceRepo,
            new FixedDateTimeProvider(),
            new StubUnitOfWork());

        var result = await handler.Handle(
            new SkipCyclicFastDayCommand(userId.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("EatDay", result.Value.OccurrenceKind);
        Assert.Equal("Active", result.Value.Status);
        Assert.Equal("Skipped", occurrence.Status.ToString());
        Assert.Contains(occurrenceRepo.StoredOccurrences, x => x.Kind == FastingOccurrenceKind.EatDay);
    }

    [Fact]
    public async Task PostponeCyclicFastDay_WhenActiveFastDay_CreatesEatDayOccurrence() {
        var userId = UserId.New();
        var plan = FastingPlan.CreateCyclic(userId, 1, 3, 16, 8, FixedNow, FixedNow);
        var occurrence = FastingOccurrence.Create(plan.Id, userId, FastingOccurrenceKind.FastDay, FixedNow, 1, 24);
        var occurrenceRepo = new InMemoryFastingOccurrenceRepository(current: occurrence);
        var handler = new PostponeCyclicFastDayCommandHandler(
            new InMemoryFastingPlanRepository(active: plan),
            occurrenceRepo,
            new FixedDateTimeProvider(),
            new StubUnitOfWork());

        var result = await handler.Handle(
            new PostponeCyclicFastDayCommand(userId.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("EatDay", result.Value.OccurrenceKind);
        Assert.Equal("Active", result.Value.Status);
        Assert.Equal("Postponed", occurrence.Status.ToString());
        Assert.Contains(occurrenceRepo.StoredOccurrences, x => x.Kind == FastingOccurrenceKind.EatDay);
    }

    private sealed class InMemoryFastingPlanRepository(FastingPlan? active = null) : IFastingPlanRepository {
        public Task<FastingPlan?> GetActiveAsync(UserId userId, bool asTracking = false, CancellationToken ct = default) => Task.FromResult(active);
        public Task<FastingPlan?> GetByIdAsync(FastingPlanId id, bool asTracking = false, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<FastingPlan>> GetByUserAsync(UserId userId, FastingPlanType? type = null, FastingPlanStatus? status = null, CancellationToken ct = default) => throw new NotSupportedException();
        public Task AddAsync(FastingPlan plan, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateAsync(FastingPlan plan, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class InMemoryFastingOccurrenceRepository(FastingOccurrence? current = null) : IFastingOccurrenceRepository {
        public List<FastingOccurrence> StoredOccurrences { get; } = current is null ? [] : [current];

        public Task<FastingOccurrence?> GetCurrentAsync(UserId userId, bool asTracking = false, CancellationToken ct = default) => Task.FromResult(StoredOccurrences.LastOrDefault(x => x.Status == FastingOccurrenceStatus.Active));
        public Task<FastingOccurrence?> GetByIdAsync(FastingOccurrenceId id, bool asTracking = false, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<FastingOccurrence>> GetByPlanAsync(FastingPlanId planId, bool includeCompleted = true, CancellationToken ct = default) {
            IReadOnlyList<FastingOccurrence> occurrences = StoredOccurrences
                .Where(x => x.PlanId == planId)
                .ToList();
            return Task.FromResult(occurrences);
        }
        public Task<IReadOnlyList<FastingOccurrence>> GetByUserAsync(UserId userId, DateTime? from = null, DateTime? to = null, FastingOccurrenceStatus? status = null, CancellationToken ct = default) => throw new NotSupportedException();
        public Task AddAsync(FastingOccurrence occurrence, CancellationToken ct = default) {
            StoredOccurrences.Add(occurrence);
            return Task.CompletedTask;
        }
        public Task UpdateAsync(FastingOccurrence occurrence, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class StubUnitOfWork : IUnitOfWork {
        public bool HasPendingChanges => false;
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class StubUserRepository(User? user) : IUserRepository {
        public Task<User?> GetByIdAsync(UserId id, CancellationToken ct = default) => Task.FromResult(user);
        public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<User?> GetByIdIncludingDeletedAsync(UserId id, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<User?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<User?> GetByTelegramUserIdIncludingDeletedAsync(long telegramUserId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<User> Items, int TotalItems)> GetPagedAsync(string? search, int page, int limit, bool includeDeleted, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<User> RecentUsers)> GetAdminDashboardSummaryAsync(int recentLimit, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Role>> GetRolesByNamesAsync(IReadOnlyList<string> names, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<User> AddAsync(User user, CancellationToken ct = default) => throw new NotSupportedException();
        public Task UpdateAsync(User user, CancellationToken ct = default) => throw new NotSupportedException();
    }

    private sealed class FixedDateTimeProvider : IDateTimeProvider {
        public DateTime UtcNow => FixedNow;
    }
}
