using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Fasting.Commands.StartFasting;
using FoodDiary.Application.Fasting.Models;
using FoodDiary.Domain.Entities.Tracking.Fasting;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Fasting;

public partial class FastingFeatureTests {
    [Fact]
    public async Task StartFasting_WithValidData_CreatesSession() {
        var user = User.Create("user@example.com", "hash");
        var planRepo = new InMemoryFastingPlanRepository();
        var occurrenceRepo = new InMemoryFastingOccurrenceRepository();
        var handler = new StartFastingCommandHandler(planRepo, occurrenceRepo, CreateCurrentUserAccessService(user), new FixedDateTimeProvider());

        Result<FastingSessionModel> result = await handler.Handle(
            new StartFastingCommand(user.Id.Value, "F16_8", PlanType: null, PlannedDurationHours: null, CyclicFastDays: null, CyclicEatDays: null, CyclicEatDayFastHours: null, CyclicEatDayEatingWindowHours: null, Notes: null), CancellationToken.None);

        ResultAssert.Success(result);
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
        var handler = new StartFastingCommandHandler(planRepo, occurrenceRepo, CreateCurrentUserAccessService(user), new FixedDateTimeProvider());

        Result<FastingSessionModel> result = await handler.Handle(
            new StartFastingCommand(user.Id.Value, "CustomIntermittent", PlanType: null, 17, CyclicFastDays: null, CyclicEatDays: null, CyclicEatDayFastHours: null, CyclicEatDayEatingWindowHours: null, Notes: null), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal("CustomIntermittent", result.Value.Protocol);
        Assert.Equal(17, result.Value.InitialPlannedDurationHours);
        Assert.Equal(17, result.Value.PlannedDurationHours);
        Assert.Equal("Active", result.Value.Status);
    }

    [Fact]
    public async Task StartFasting_WithExtendedProtocol_CreatesFastDaySession() {
        var user = User.Create("extended-start@example.com", "hash");
        var planRepo = new InMemoryFastingPlanRepository();
        var occurrenceRepo = new InMemoryFastingOccurrenceRepository();
        var handler = new StartFastingCommandHandler(planRepo, occurrenceRepo, CreateCurrentUserAccessService(user), new FixedDateTimeProvider());

        Result<FastingSessionModel> result = await handler.Handle(
            new StartFastingCommand(user.Id.Value, "F36_0", PlanType: null, PlannedDurationHours: null, CyclicFastDays: null, CyclicEatDays: null, CyclicEatDayFastHours: null, CyclicEatDayEatingWindowHours: null, "extended notes"),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal("F36_0", result.Value.Protocol);
        Assert.Equal("Extended", result.Value.PlanType);
        Assert.Equal("FastDay", result.Value.OccurrenceKind);
        Assert.Equal(36, result.Value.InitialPlannedDurationHours);
        Assert.Equal("extended notes", result.Value.Notes);
        Assert.Single(planRepo.StoredPlans);
        Assert.Single(occurrenceRepo.StoredOccurrences);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(24)]
    [InlineData(30)]
    public async Task StartFasting_WithInvalidCustomIntermittentDuration_ReturnsFailure(int duration) {
        var user = User.Create("user@example.com", "hash");
        var planRepo = new InMemoryFastingPlanRepository();
        var occurrenceRepo = new InMemoryFastingOccurrenceRepository();
        var handler = new StartFastingCommandHandler(planRepo, occurrenceRepo, CreateCurrentUserAccessService(user), new FixedDateTimeProvider());

        Result<FastingSessionModel> result = await handler.Handle(
            new StartFastingCommand(user.Id.Value, "CustomIntermittent", PlanType: null, duration, CyclicFastDays: null, CyclicEatDays: null, CyclicEatDayFastHours: null, CyclicEatDayEatingWindowHours: null, Notes: null), CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task StartFasting_WhenAlreadyActive_ReturnsFailure() {
        var user = User.Create("user@example.com", "hash");
        var existingPlan = FastingPlan.CreateIntermittent(user.Id, FastingProtocol.F16_8, 16, 8, FixedNow);
        var planRepo = new InMemoryFastingPlanRepository(active: existingPlan);
        var occurrenceRepo = new InMemoryFastingOccurrenceRepository();
        var handler = new StartFastingCommandHandler(planRepo, occurrenceRepo, CreateCurrentUserAccessService(user), new FixedDateTimeProvider());

        Result<FastingSessionModel> result = await handler.Handle(
            new StartFastingCommand(user.Id.Value, "F18_6", PlanType: null, PlannedDurationHours: null, CyclicFastDays: null, CyclicEatDays: null, CyclicEatDayFastHours: null, CyclicEatDayEatingWindowHours: null, Notes: null), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Contains("AlreadyActive", result.Error.Code, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StartFasting_WithInvalidProtocol_ReturnsFailure() {
        var user = User.Create("user@example.com", "hash");
        var handler = new StartFastingCommandHandler(
            new InMemoryFastingPlanRepository(), new InMemoryFastingOccurrenceRepository(), CreateCurrentUserAccessService(user), new FixedDateTimeProvider());

        Result<FastingSessionModel> result = await handler.Handle(
            new StartFastingCommand(user.Id.Value, "InvalidProtocol", PlanType: null, PlannedDurationHours: null, CyclicFastDays: null, CyclicEatDays: null, CyclicEatDayFastHours: null, CyclicEatDayEatingWindowHours: null, Notes: null), CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task StartFasting_WithMissingProtocol_ReturnsFailure() {
        var user = User.Create("missing-protocol@example.com", "hash");
        var handler = new StartFastingCommandHandler(
            new InMemoryFastingPlanRepository(),
            new InMemoryFastingOccurrenceRepository(),
            CreateCurrentUserAccessService(user),
            new FixedDateTimeProvider());

        Result<FastingSessionModel> result = await handler.Handle(
            new StartFastingCommand(user.Id.Value, Protocol: null, PlanType: null, PlannedDurationHours: null, CyclicFastDays: null, CyclicEatDays: null, CyclicEatDayFastHours: null, CyclicEatDayEatingWindowHours: null, Notes: null),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Fasting.InvalidProtocol", result.Error.Code);
    }

    [Fact]
    public async Task StartFasting_WithBlankIntermittentProtocol_ReturnsInvalidProtocol() {
        User user = CreateUser(UserId.New());
        var planRepo = new InMemoryFastingPlanRepository();
        var occurrenceRepo = new InMemoryFastingOccurrenceRepository();
        var handler = new StartFastingCommandHandler(
            planRepo,
            occurrenceRepo,
            CreateCurrentUserAccessService(user),
            new FixedDateTimeProvider());

        Result<FastingSessionModel> result = await handler.Handle(
            new StartFastingCommand(user.Id.Value, "   ", "Intermittent", PlannedDurationHours: null, CyclicFastDays: null, CyclicEatDays: null, CyclicEatDayFastHours: null, CyclicEatDayEatingWindowHours: null, Notes: null),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Fasting.InvalidProtocol", result.Error.Code);
        Assert.Empty(planRepo.StoredPlans);
        Assert.Empty(occurrenceRepo.StoredOccurrences);
    }

    [Fact]
    public async Task StartFasting_WithExplicitIntermittentInvalidProtocol_ReturnsInvalidProtocol() {
        User user = CreateUser(UserId.New());
        var planRepo = new InMemoryFastingPlanRepository();
        var occurrenceRepo = new InMemoryFastingOccurrenceRepository();
        var handler = new StartFastingCommandHandler(
            planRepo,
            occurrenceRepo,
            CreateCurrentUserAccessService(user),
            new FixedDateTimeProvider());

        Result<FastingSessionModel> result = await handler.Handle(
            new StartFastingCommand(user.Id.Value, "not-a-protocol", "Intermittent", PlannedDurationHours: null, CyclicFastDays: null, CyclicEatDays: null, CyclicEatDayFastHours: null, CyclicEatDayEatingWindowHours: null, Notes: null),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Fasting.InvalidProtocol", result.Error.Code);
        Assert.Empty(planRepo.StoredPlans);
        Assert.Empty(occurrenceRepo.StoredOccurrences);
    }

    [Fact]
    public async Task StartFasting_WithInvalidPlanType_ReturnsFailure() {
        var user = User.Create("user@example.com", "hash");
        var planRepo = new InMemoryFastingPlanRepository();
        var occurrenceRepo = new InMemoryFastingOccurrenceRepository();
        var handler = new StartFastingCommandHandler(
            planRepo,
            occurrenceRepo,
            CreateCurrentUserAccessService(user),
            new FixedDateTimeProvider());

        Result<FastingSessionModel> result = await handler.Handle(
            new StartFastingCommand(user.Id.Value, "F16_8", "InvalidPlanType", PlannedDurationHours: null, CyclicFastDays: null, CyclicEatDays: null, CyclicEatDayFastHours: null, CyclicEatDayEatingWindowHours: null, Notes: null),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Empty(planRepo.StoredPlans);
        Assert.Empty(occurrenceRepo.StoredOccurrences);
    }

    [Fact]
    public async Task StartFasting_WithUndefinedNumericPlanType_ReturnsFailure() {
        User user = CreateUser(UserId.New());
        var planRepo = new InMemoryFastingPlanRepository();
        var occurrenceRepo = new InMemoryFastingOccurrenceRepository();
        var handler = new StartFastingCommandHandler(
            planRepo,
            occurrenceRepo,
            CreateCurrentUserAccessService(user),
            new FixedDateTimeProvider());

        Result<FastingSessionModel> result = await handler.Handle(
            new StartFastingCommand(user.Id.Value, "F16_8", "999", PlannedDurationHours: null, CyclicFastDays: null, CyclicEatDays: null, CyclicEatDayFastHours: null, CyclicEatDayEatingWindowHours: null, Notes: null),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Fasting.InvalidProtocol", result.Error.Code);
        Assert.Empty(planRepo.StoredPlans);
        Assert.Empty(occurrenceRepo.StoredOccurrences);
    }

    [Fact]
    public async Task StartFasting_WithExtendedPlanTypeAndInvalidProtocol_ReturnsInvalidProtocol() {
        User user = CreateUser(UserId.New());
        var planRepo = new InMemoryFastingPlanRepository();
        var occurrenceRepo = new InMemoryFastingOccurrenceRepository();
        var handler = new StartFastingCommandHandler(
            planRepo,
            occurrenceRepo,
            CreateCurrentUserAccessService(user),
            new FixedDateTimeProvider());

        Result<FastingSessionModel> result = await handler.Handle(
            new StartFastingCommand(user.Id.Value, "not-a-protocol", "Extended", PlannedDurationHours: null, CyclicFastDays: null, CyclicEatDays: null, CyclicEatDayFastHours: null, CyclicEatDayEatingWindowHours: null, Notes: null),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Fasting.InvalidProtocol", result.Error.Code);
        Assert.Empty(planRepo.StoredPlans);
        Assert.Empty(occurrenceRepo.StoredOccurrences);
    }

    [Fact]
    public async Task StartFasting_WithDeletedUser_ReturnsAccountDeleted() {
        User user = CreateUser(UserId.New());
        user.DeleteAccount(FixedNow);
        var planRepo = new InMemoryFastingPlanRepository();
        var occurrenceRepo = new InMemoryFastingOccurrenceRepository();
        var handler = new StartFastingCommandHandler(
            planRepo,
            occurrenceRepo,
            CreateCurrentUserAccessService(user),
            new FixedDateTimeProvider());

        Result<FastingSessionModel> result = await handler.Handle(
            new StartFastingCommand(user.Id.Value, "F16_8", PlanType: null, PlannedDurationHours: null, CyclicFastDays: null, CyclicEatDays: null, CyclicEatDayFastHours: null, CyclicEatDayEatingWindowHours: null, Notes: null),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
        Assert.Empty(planRepo.StoredPlans);
        Assert.Empty(occurrenceRepo.StoredOccurrences);
    }

    [Fact]
    public async Task StartFasting_WithNullUserId_ReturnsFailure() {
        var handler = new StartFastingCommandHandler(
            new InMemoryFastingPlanRepository(), new InMemoryFastingOccurrenceRepository(), CreateCurrentUserAccessService(user: null), new FixedDateTimeProvider());

        Result<FastingSessionModel> result = await handler.Handle(
            new StartFastingCommand(UserId: null, "F16_8", PlanType: null, PlannedDurationHours: null, CyclicFastDays: null, CyclicEatDays: null, CyclicEatDayFastHours: null, CyclicEatDayEatingWindowHours: null, Notes: null), CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task StartFasting_WithCyclicPlan_CreatesCyclicSession() {
        var user = User.Create("user@example.com", "hash");
        var planRepo = new InMemoryFastingPlanRepository();
        var occurrenceRepo = new InMemoryFastingOccurrenceRepository();
        var handler = new StartFastingCommandHandler(planRepo, occurrenceRepo, CreateCurrentUserAccessService(user), new FixedDateTimeProvider());

        Result<FastingSessionModel> result = await handler.Handle(
            new StartFastingCommand(user.Id.Value, Protocol: null, "Cyclic", PlannedDurationHours: null, 1, 3, 16, 8, Notes: null), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal("Cyclic", result.Value.PlanType);
        Assert.Equal("FastDay", result.Value.OccurrenceKind);
        Assert.Equal(1, result.Value.CyclicFastDays);
        Assert.Equal(3, result.Value.CyclicEatDays);
    }

    [Fact]
    public async Task StartFasting_WithInvalidCyclicDays_ReturnsInvalidProtocol() {
        User user = CreateUser(UserId.New());
        var planRepo = new InMemoryFastingPlanRepository();
        var occurrenceRepo = new InMemoryFastingOccurrenceRepository();
        var handler = new StartFastingCommandHandler(
            planRepo,
            occurrenceRepo,
            CreateCurrentUserAccessService(user),
            new FixedDateTimeProvider());

        Result<FastingSessionModel> result = await handler.Handle(
            new StartFastingCommand(user.Id.Value, Protocol: null, "Cyclic", PlannedDurationHours: null, 0, 3, 16, 8, Notes: null),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Fasting.InvalidProtocol", result.Error.Code);
        Assert.Empty(planRepo.StoredPlans);
        Assert.Empty(occurrenceRepo.StoredOccurrences);
    }

    [Fact]
    public async Task StartFasting_WithExtendedPlanTypeAndIntermittentProtocol_ReturnsInvalidProtocol() {
        User user = CreateUser(UserId.New());
        var planRepo = new InMemoryFastingPlanRepository();
        var occurrenceRepo = new InMemoryFastingOccurrenceRepository();
        var handler = new StartFastingCommandHandler(
            planRepo,
            occurrenceRepo,
            CreateCurrentUserAccessService(user),
            new FixedDateTimeProvider());

        Result<FastingSessionModel> result = await handler.Handle(
            new StartFastingCommand(user.Id.Value, "F16_8", "Extended", PlannedDurationHours: null, CyclicFastDays: null, CyclicEatDays: null, CyclicEatDayFastHours: null, CyclicEatDayEatingWindowHours: null, Notes: null),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Fasting.InvalidProtocol", result.Error.Code);
        Assert.Empty(planRepo.StoredPlans);
        Assert.Empty(occurrenceRepo.StoredOccurrences);
    }


}
