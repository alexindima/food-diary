using System.Globalization;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FluentValidation.TestHelper;
using FoodDiary.Application.Abstractions.Audit.Common;
using FoodDiary.Application.Abstractions.Audit.Models;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Abstractions.Dietologist.Models;
using FoodDiary.Application.Consumptions.Models;
using FoodDiary.Application.Dashboard.Models;
using FoodDiary.Application.Dietologist.Commands.SetAttentionSignalState;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Application.Dietologist.Queries.GetAttentionSignals;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.WeightEntries.Models;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

#pragma warning disable MA0003

namespace FoodDiary.Application.Tests.Dietologist;

[ExcludeFromCodeCoverage]
public sealed class AttentionSignalTests {
    private static readonly DateTime UtcNow = new(2026, 7, 25, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetAttentionSignals_WhenCurrentUserAccessFails_ReturnsFailure() {
        IUserContextService userContext = Substitute.For<IUserContextService>();
        userContext.EnsureCanAccessAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(Errors.Authentication.InvalidToken);
        GetAttentionSignalsQueryHandler handler = CreateQueryHandler(userContext: userContext);

        Result<IReadOnlyList<AttentionSignalModel>> result = await handler.Handle(
            CreateQuery(UserId.New().Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal(Errors.Authentication.InvalidToken.Code, result.Error.Code);
    }

    [Fact]
    public async Task GetAttentionSignals_WhenClientLookupFails_ReturnsFailure() {
        var user = User.Create("dietologist@example.com", "hash");
        IDietologistInvitationReadService invitations = Substitute.For<IDietologistInvitationReadService>();
        invitations.GetMyClientsAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyList<ClientSummaryModel>>(Errors.Dietologist.AccessDenied));
        GetAttentionSignalsQueryHandler handler = CreateQueryHandler(invitations, userContext: CreateUserContext(user));

        Result<IReadOnlyList<AttentionSignalModel>> result = await handler.Handle(
            CreateQuery(user.Id.Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal(Errors.Dietologist.AccessDenied.Code, result.Error.Code);
    }

    [Fact]
    public async Task GetAttentionSignals_CreatesAndOrdersAllSupportedSignals() {
        var user = User.Create("dietologist@example.com", "hash");
        ClientSummaryModel client = CreateClient(
            acceptedAtUtc: UtcNow.AddDays(-20),
            firstName: "Alex",
            lastName: "Client");
        DashboardSnapshotModel dashboard = CreateDashboard(
            meals: [CreateMeal(UtcNow.AddDays(-10))],
            dailyGoal: 2000,
            weeklyCalories: [
                new DailyCaloriesModel(UtcNow.AddDays(-1), 1000),
                new DailyCaloriesModel(UtcNow.AddDays(-2), 1000),
            ],
            weightTrend: [
                new WeightEntrySummaryModel(UtcNow.AddDays(-14), UtcNow.AddDays(-8), 100),
                new WeightEntrySummaryModel(UtcNow.AddDays(-7), UtcNow, 90),
            ]);
        GetAttentionSignalsQueryHandler handler = CreateQueryHandler(
            CreateInvitationService(user.Id, client),
            CreateDashboardService(Result.Success(dashboard)),
            userContext: CreateUserContext(user));

        Result<IReadOnlyList<AttentionSignalModel>> result = await handler.Handle(
            CreateQuery(user.Id.Value),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Collection(
            result.Value,
            signal => Assert.Multiple(
                () => Assert.Equal("MaterialWeightChange", signal.Type),
                () => Assert.Equal("High", signal.Severity),
                () => Assert.Equal("Alex Client", signal.ClientDisplayName)),
            signal => Assert.Multiple(
                () => Assert.Equal("CalorieTargetDeviation", signal.Type),
                () => Assert.Equal("High", signal.Severity)),
            signal => Assert.Multiple(
                () => Assert.Equal("DiaryInactivity", signal.Type),
                () => Assert.Equal("High", signal.Severity)));
    }

    [Fact]
    public async Task GetAttentionSignals_UsesEmailAndAcceptedDateWhenClientHasNoMealsOrName() {
        var user = User.Create("dietologist@example.com", "hash");
        ClientSummaryModel client = CreateClient(
            acceptedAtUtc: UtcNow.AddDays(-5),
            firstName: null,
            lastName: null,
            permissions: CreatePermissions(meals: true, statistics: false, weight: false));
        GetAttentionSignalsQueryHandler handler = CreateQueryHandler(
            CreateInvitationService(user.Id, client),
            CreateDashboardService(Result.Success(CreateDashboard())),
            userContext: CreateUserContext(user));

        Result<IReadOnlyList<AttentionSignalModel>> result = await handler.Handle(
            CreateQuery(user.Id.Value) with { InactivityDays = 3 },
            CancellationToken.None);

        ResultAssert.Success(result);
        AttentionSignalModel signal = Assert.Single(result.Value);
        Assert.Multiple(
            () => Assert.Equal("client@example.com", signal.ClientDisplayName),
            () => Assert.Equal("InsufficientDiaryData", signal.Reason),
            () => Assert.Equal(client.AcceptedAtUtc, signal.DetectedAtUtc));
    }

    [Theory]
    [InlineData("dietologist.attention.acknowledged", null, false)]
    [InlineData("dietologist.attention.snoozed", "2026-07-26T12:00:00.0000000Z", false)]
    [InlineData("dietologist.attention.snoozed", "2026-07-24T12:00:00.0000000Z", true)]
    [InlineData("dietologist.attention.snoozed", "not-a-date", true)]
    public async Task GetAttentionSignals_AppliesLatestSignalState(
        string action,
        string? metadata,
        bool expectedVisible) {
        var user = User.Create("dietologist@example.com", "hash");
        ClientSummaryModel client = CreateClient(
            acceptedAtUtc: UtcNow.AddDays(-5),
            permissions: CreatePermissions(meals: true, statistics: false, weight: false));
        string signalId = string.Create(
            CultureInfo.InvariantCulture,
            $"DiaryInactivity:{client.UserId:N}:{client.AcceptedAtUtc:yyyyMMdd}");
        IAuditEntryReadService audits = Substitute.For<IAuditEntryReadService>();
        audits.GetRecentAsync(null, 500, Arg.Any<CancellationToken>())
            .Returns([
                new AuditEntryReadModel(
                    Guid.NewGuid(),
                    user.Id.Value,
                    client.UserId,
                    action,
                    "AttentionSignal",
                    signalId,
                    metadata,
                    UtcNow),
            ]);
        GetAttentionSignalsQueryHandler handler = CreateQueryHandler(
            CreateInvitationService(user.Id, client),
            CreateDashboardService(Result.Success(CreateDashboard())),
            audits,
            CreateUserContext(user));

        Result<IReadOnlyList<AttentionSignalModel>> result = await handler.Handle(
            CreateQuery(user.Id.Value) with { InactivityDays = 3 },
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(expectedVisible, result.Value.Count == 1);
    }

    [Fact]
    public async Task GetAttentionSignals_IgnoresFailedDashboardAndInsufficientMetrics() {
        var user = User.Create("dietologist@example.com", "hash");
        ClientSummaryModel first = CreateClient(UtcNow.AddDays(-1));
        ClientSummaryModel second = CreateClient(UtcNow.AddDays(-1));
        ILegacyDashboardTestService dashboards = Substitute.For<ILegacyDashboardTestService>();
        dashboards.GetDashboardAsync(
                Arg.Any<UserId>(),
                first.UserId,
                Arg.Any<DateTime>(),
                Arg.Any<DateTime?>(),
                Arg.Any<string>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Failure<DashboardSnapshotModel>(Errors.Dietologist.AccessDenied));
        dashboards.GetDashboardAsync(
                Arg.Any<UserId>(),
                second.UserId,
                Arg.Any<DateTime>(),
                Arg.Any<DateTime?>(),
                Arg.Any<string>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Success(CreateDashboard(
                dailyGoal: 0,
                weeklyCalories: [new DailyCaloriesModel(UtcNow, 100)],
                weightTrend: [new WeightEntrySummaryModel(UtcNow, UtcNow, 0)])));
        GetAttentionSignalsQueryHandler handler = CreateQueryHandler(
            CreateInvitationService(user.Id, first, second),
            dashboards,
            userContext: CreateUserContext(user));

        Result<IReadOnlyList<AttentionSignalModel>> result = await handler.Handle(
            CreateQuery(user.Id.Value),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task GetAttentionSignals_DoesNotCreateSignalsBelowConfiguredThresholds() {
        var user = User.Create("dietologist@example.com", "hash");
        ClientSummaryModel first = CreateClient(
            UtcNow.AddDays(-1),
            permissions: CreatePermissions(meals: false, statistics: true, weight: true));
        ClientSummaryModel second = CreateClient(
            UtcNow.AddDays(-1),
            permissions: CreatePermissions(meals: false, statistics: true, weight: true));
        ILegacyDashboardTestService dashboards = Substitute.For<ILegacyDashboardTestService>();
        dashboards.GetDashboardAsync(
                Arg.Any<UserId>(),
                first.UserId,
                Arg.Any<DateTime>(),
                Arg.Any<DateTime?>(),
                Arg.Any<string>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Success(CreateDashboard(
                weeklyCalories: [new DailyCaloriesModel(UtcNow, 1000)],
                weightTrend: [
                    new WeightEntrySummaryModel(UtcNow.AddDays(-2), UtcNow.AddDays(-1), 100),
                    new WeightEntrySummaryModel(UtcNow, UtcNow, 99),
                ])));
        dashboards.GetDashboardAsync(
                Arg.Any<UserId>(),
                second.UserId,
                Arg.Any<DateTime>(),
                Arg.Any<DateTime?>(),
                Arg.Any<string>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Success(CreateDashboard(
                weeklyCalories: [
                    new DailyCaloriesModel(UtcNow, 2000),
                    new DailyCaloriesModel(UtcNow.AddDays(-1), 1000),
                ],
                weightTrend: [])));
        GetAttentionSignalsQueryHandler handler = CreateQueryHandler(
            CreateInvitationService(user.Id, first, second),
            dashboards,
            userContext: CreateUserContext(user));

        Result<IReadOnlyList<AttentionSignalModel>> result = await handler.Handle(
            CreateQuery(user.Id.Value),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task SetAttentionSignalState_Acknowledge_WritesAuditEntry() {
        var user = User.Create("dietologist@example.com", "hash");
        var clientId = UserId.New();
        IAuditEntryWriter writer = Substitute.For<IAuditEntryWriter>();
        var handler = new SetAttentionSignalStateCommandHandler(
            CreateActiveInvitationRepository(user.Id, clientId),
            writer,
            CreateUserContext(user),
            new FixedTimeProvider(UtcNow));

        Result result = await handler.Handle(
            new SetAttentionSignalStateCommand(user.Id.Value, clientId.Value, "signal", "Acknowledge", null),
            CancellationToken.None);

        ResultAssert.Success(result);
        await writer.Received(1).AddAsync(
            user.Id,
            clientId.Value,
            "dietologist.attention.acknowledged",
            "AttentionSignal",
            "signal",
            null,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetAttentionSignalState_Snooze_WritesUtcMetadata() {
        var user = User.Create("dietologist@example.com", "hash");
        var clientId = UserId.New();
        IAuditEntryWriter writer = Substitute.For<IAuditEntryWriter>();
        var handler = new SetAttentionSignalStateCommandHandler(
            CreateActiveInvitationRepository(user.Id, clientId),
            writer,
            CreateUserContext(user),
            new FixedTimeProvider(UtcNow));
        DateTime localEnd = new(2026, 7, 26, 16, 0, 0, DateTimeKind.Local);

        Result result = await handler.Handle(
            new SetAttentionSignalStateCommand(user.Id.Value, clientId.Value, "signal", "snooze", localEnd),
            CancellationToken.None);

        ResultAssert.Success(result);
        await writer.Received(1).AddAsync(
            user.Id,
            clientId.Value,
            "dietologist.attention.snoozed",
            "AttentionSignal",
            "signal",
            localEnd.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetAttentionSignalState_RejectsInvalidClientAccessAndPastSnooze() {
        var user = User.Create("dietologist@example.com", "hash");
        var handler = new SetAttentionSignalStateCommandHandler(
            Substitute.For<IDietologistInvitationReadModelRepository>(),
            Substitute.For<IAuditEntryWriter>(),
            CreateUserContext(user),
            new FixedTimeProvider(UtcNow));

        Result emptyClient = await handler.Handle(
            new SetAttentionSignalStateCommand(user.Id.Value, Guid.Empty, "signal", "Acknowledge", null),
            CancellationToken.None);
        Result denied = await handler.Handle(
            new SetAttentionSignalStateCommand(user.Id.Value, Guid.NewGuid(), "signal", "Acknowledge", null),
            CancellationToken.None);
        var activeClient = UserId.New();
        IDietologistInvitationReadModelRepository active = CreateActiveInvitationRepository(user.Id, activeClient);
        var pastHandler = new SetAttentionSignalStateCommandHandler(
            active,
            Substitute.For<IAuditEntryWriter>(),
            CreateUserContext(user),
            new FixedTimeProvider(UtcNow));
        Result past = await pastHandler.Handle(
            new SetAttentionSignalStateCommand(user.Id.Value, activeClient.Value, "signal", "Snooze", UtcNow),
            CancellationToken.None);

        Assert.Multiple(
            () => ResultAssert.Failure(emptyClient),
            () => ResultAssert.Failure(denied),
            () => ResultAssert.Failure(past));
    }

    [Fact]
    public async Task SetAttentionSignalState_WhenCurrentUserAccessFails_ReturnsFailure() {
        IUserContextService userContext = Substitute.For<IUserContextService>();
        userContext.EnsureCanAccessAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(Errors.Authentication.InvalidToken);
        var handler = new SetAttentionSignalStateCommandHandler(
            Substitute.For<IDietologistInvitationReadModelRepository>(),
            Substitute.For<IAuditEntryWriter>(),
            userContext,
            new FixedTimeProvider(UtcNow));

        Result result = await handler.Handle(
            new SetAttentionSignalStateCommand(Guid.NewGuid(), Guid.NewGuid(), "signal", "Acknowledge", null),
            CancellationToken.None);

        ResultAssert.Failure(result, Errors.Authentication.InvalidToken.Code);
    }

    [Fact]
    public void SetAttentionSignalStateValidator_ValidatesShape() {
        var validator = new SetAttentionSignalStateCommandValidator();

        TestValidationResult<SetAttentionSignalStateCommand> invalid = validator.TestValidate(
            new SetAttentionSignalStateCommand(null, Guid.Empty, "", "Later", null));
        TestValidationResult<SetAttentionSignalStateCommand> missingSnoozeEnd = validator.TestValidate(
            new SetAttentionSignalStateCommand(null, Guid.NewGuid(), "signal", "Snooze", null));
        TestValidationResult<SetAttentionSignalStateCommand> valid = validator.TestValidate(
            new SetAttentionSignalStateCommand(null, Guid.NewGuid(), "signal", "acknowledge", null));

        Assert.Multiple(
            () => invalid.ShouldHaveValidationErrorFor(command => command.ClientUserId),
            () => invalid.ShouldHaveValidationErrorFor(command => command.SignalId),
            () => invalid.ShouldHaveValidationErrorFor(command => command.Action),
            () => missingSnoozeEnd.ShouldHaveValidationErrorFor(command => command.SnoozedUntilUtc),
            () => valid.ShouldNotHaveAnyValidationErrors());
    }

    private static GetAttentionSignalsQueryHandler CreateQueryHandler(
        IDietologistInvitationReadService? invitations = null,
        ILegacyDashboardTestService? dashboards = null,
        IAuditEntryReadService? audits = null,
        IUserContextService? userContext = null) {
        if (invitations is null) {
            invitations = Substitute.For<IDietologistInvitationReadService>();
            invitations.GetMyClientsAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
                .Returns(Result.Success<IReadOnlyList<ClientSummaryModel>>([]));
        }

        if (audits is null) {
            audits = Substitute.For<IAuditEntryReadService>();
            audits.GetRecentAsync(null, 500, Arg.Any<CancellationToken>())
                .Returns([]);
        }

        return new GetAttentionSignalsQueryHandler(
            invitations,
            CreateMetricsService(dashboards),
            audits,
            userContext ?? Substitute.For<IUserContextService>(),
            new FixedTimeProvider(UtcNow));
    }

    private static IAttentionSignalMetricsReadService CreateMetricsService(
        ILegacyDashboardTestService? dashboards) {
        IAttentionSignalMetricsReadService service = Substitute.For<IAttentionSignalMetricsReadService>();
        service.GetAsync(
                Arg.Any<IReadOnlyCollection<UserId>>(),
                Arg.Any<DateTime>(),
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(call => BuildMetricsAsync(
                dashboards,
                call.ArgAt<IReadOnlyCollection<UserId>>(0),
                call.ArgAt<DateTime>(1),
                call.ArgAt<DateTime>(2),
                call.ArgAt<CancellationToken>(3)));
        return service;
    }

    private static async Task<IReadOnlyList<AttentionSignalMetricsReadModel>> BuildMetricsAsync(
        ILegacyDashboardTestService? dashboards,
        IReadOnlyCollection<UserId> clientIds,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken cancellationToken) {
        if (dashboards is null) {
            return [];
        }

        var metrics = new List<AttentionSignalMetricsReadModel>();
        foreach (UserId clientId in clientIds) {
            Result<DashboardSnapshotModel> result = await dashboards.GetDashboardAsync(
                UserId.Empty,
                clientId.Value,
                dateFrom,
                dateTo,
                "en",
                90,
                1,
                100,
                cancellationToken).ConfigureAwait(false);
            if (result.IsFailure) {
                continue;
            }

            DashboardSnapshotModel dashboard = result.Value;
            metrics.Add(new AttentionSignalMetricsReadModel(
                clientId.Value,
                dashboard.DailyGoal,
                dashboard.Meals.Items.Count == 0 ? null : dashboard.Meals.Items.Max(item => item.Date),
                [
                    .. dashboard.WeeklyCalories.Select(item =>
                        new AttentionSignalDailyCaloriesReadModel(item.Date, item.Calories)),
                ],
                [
                    .. (dashboard.WeightTrend ?? []).Select(item =>
                        new AttentionSignalWeightPointReadModel(item.EndDate, item.AverageWeight)),
                ]));
        }

        return metrics;
    }

    private static GetAttentionSignalsQuery CreateQuery(Guid userId) =>
        new(userId, 3, 25, 2, 5, 14);

    private static IDietologistInvitationReadService CreateInvitationService(
        UserId dietologistId,
        params ClientSummaryModel[] clients) {
        IDietologistInvitationReadService service = Substitute.For<IDietologistInvitationReadService>();
        service.GetMyClientsAsync(dietologistId, Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyList<ClientSummaryModel>>(clients));
        return service;
    }

    private static ILegacyDashboardTestService CreateDashboardService(Result<DashboardSnapshotModel> result) {
        ILegacyDashboardTestService service = Substitute.For<ILegacyDashboardTestService>();
        service.GetDashboardAsync(
                Arg.Any<UserId>(),
                Arg.Any<Guid>(),
                Arg.Any<DateTime>(),
                Arg.Any<DateTime?>(),
                Arg.Any<string>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(result);
        return service;
    }

    private static IUserContextService CreateUserContext(User user) {
        IUserContextService service = Substitute.For<IUserContextService>();
        service.EnsureCanAccessAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns((Error?)null);
        service.GetAccessibleUserAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(Result.Success(user));
        return service;
    }

    private static IDietologistInvitationReadModelRepository CreateActiveInvitationRepository(
        UserId dietologistId,
        UserId clientId) {
        IDietologistInvitationReadModelRepository repository = Substitute.For<IDietologistInvitationReadModelRepository>();
        repository.GetActiveByClientAndDietologistReadModelAsync(
                clientId,
                dietologistId,
                Arg.Any<CancellationToken>())
            .Returns(new DietologistInvitationReadModel(
                Guid.NewGuid(),
                clientId.Value,
                dietologistId.Value,
                "dietologist@example.com",
                "client@example.com",
                null,
                null,
                null,
                null,
                null,
                null,
                ActivityLevel.Moderate,
                "dietologist@example.com",
                null,
                null,
                DietologistInvitationStatus.Accepted,
                new DietologistPermissionsReadModel(true, true, true, true, true, true, true, true),
                UtcNow.AddDays(-10),
                UtcNow.AddDays(10),
                UtcNow.AddDays(-9)));
        return repository;
    }

    private static ClientSummaryModel CreateClient(
        DateTime acceptedAtUtc,
        string? firstName = "Client",
        string? lastName = null,
        DietologistPermissionsModel? permissions = null) =>
        new(
            Guid.NewGuid(),
            "client@example.com",
            firstName,
            lastName,
            null,
            null,
            null,
            null,
            null,
            permissions ?? CreatePermissions(true, true, true),
            acceptedAtUtc);

    private static DietologistPermissionsModel CreatePermissions(bool meals, bool statistics, bool weight) =>
        new(meals, statistics, weight, false, false, false, false, false);

    private static DashboardSnapshotModel CreateDashboard(
        IReadOnlyList<ConsumptionModel>? meals = null,
        double dailyGoal = 2000,
        IReadOnlyList<DailyCaloriesModel>? weeklyCalories = null,
        IReadOnlyList<WeightEntrySummaryModel>? weightTrend = null) =>
        new(
            UtcNow.AddDays(-13),
            UtcNow,
            dailyGoal,
            dailyGoal * 7,
            new DashboardStatisticsModel(0, 0, 0, 0, 0, null, null, null, null),
            weeklyCalories ?? [],
            new DashboardWeightModel(null, null, null),
            new DashboardWaistModel(null, null, null),
            new DashboardMealsModel(meals ?? [], meals?.Count ?? 0),
            WeightTrend: weightTrend);

    private static ConsumptionModel CreateMeal(DateTime date) =>
        new(
            Guid.NewGuid(),
            date,
            null,
            null,
            null,
            null,
            0,
            0,
            0,
            0,
            0,
            0,
            false,
            null,
            null,
            null,
            null,
            null,
            null,
            0,
            0,
            0,
            "",
            false,
            null,
            [],
            []);

    public interface ILegacyDashboardTestService {
        Task<Result<DashboardSnapshotModel>> GetDashboardAsync(
            UserId dietologistUserId,
            Guid clientUserId,
            DateTime date,
            DateTime? dateTo,
            string locale,
            int trendDays,
            int page,
            int pageSize,
            CancellationToken cancellationToken);
    }

    [ExcludeFromCodeCoverage]
    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(utcNow, TimeSpan.Zero);
    }
}
