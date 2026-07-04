using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Fasting.Models;
using FoodDiary.Application.Fasting.Queries.GetCurrentFasting;
using FoodDiary.Application.Fasting.Queries.GetFastingHistory;
using FoodDiary.Application.Fasting.Queries.GetFastingInsights;
using FoodDiary.Application.Fasting.Queries.GetFastingOverview;
using FoodDiary.Application.Fasting.Queries.GetFastingStats;
using FoodDiary.Application.Fasting.Services;
using FoodDiary.Domain.Entities.Tracking.Fasting;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Fasting;

public partial class FastingFeatureTests {
    [Fact]
    public async Task GetFastingInsights_WithCurrentAndHistory_ReturnsInsightsAndPrompt() {
        var userId = UserId.New();
        var plan = FastingPlan.CreateIntermittent(userId, FastingProtocol.F16_8, 16, 8, FixedNow.AddDays(-5));
        var current = FastingOccurrence.Create(
            plan.Id,
            userId,
            FastingOccurrenceKind.FastingWindow,
            FixedNow.AddHours(-13),
            1,
            16);
        var historyOne = FastingOccurrence.Create(
            plan.Id,
            userId,
            FastingOccurrenceKind.FastingWindow,
            FixedNow.AddDays(-3),
            1,
            16);
        historyOne.UpdateCheckIn(5, 5, 5, ["headache"], "note", FixedNow.AddDays(-3).AddHours(8));
        historyOne.Complete(FixedNow.AddDays(-3).AddHours(16));

        var historyTwo = FastingOccurrence.Create(
            plan.Id,
            userId,
            FastingOccurrenceKind.FastingWindow,
            FixedNow.AddDays(-2),
            1,
            16);
        historyTwo.UpdateCheckIn(4, 4, 4, ["headache"], "note", FixedNow.AddDays(-2).AddHours(8));
        historyTwo.Complete(FixedNow.AddDays(-2).AddHours(16));

        var occurrenceRepo = new InMemoryFastingOccurrenceRepository(current: current);
        occurrenceRepo.StoredOccurrences.InsertRange(0, [historyOne, historyTwo]);
        var handler = new GetFastingInsightsQueryHandler(
            occurrenceRepo,
            new FastingAnalyticsService(occurrenceRepo, new InMemoryFastingCheckInRepository()),
            CreateCurrentUserAccessService(userId),
            new FixedDateTimeProvider());

        Result<FastingInsightsModel> result = await handler.Handle(new GetFastingInsightsQuery(userId.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Contains(result.Value.Alerts, x => string.Equals(x.Id, "mid", StringComparison.Ordinal));
        Assert.Contains(result.Value.Insights, x => string.Equals(x.Id, "symptom-headache", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetFastingInsights_WithLateCurrentWithoutCheckIn_ReturnsLateAlert() {
        var userId = UserId.New();
        var current = FastingOccurrence.Create(
            FastingPlanId.New(),
            userId,
            FastingOccurrenceKind.FastDay,
            FixedNow.AddHours(-21),
            sequenceNumber: 1,
            targetHours: 36);
        var occurrenceRepo = new InMemoryFastingOccurrenceRepository(current);
        var handler = new GetFastingInsightsQueryHandler(
            occurrenceRepo,
            new FastingAnalyticsService(occurrenceRepo, new InMemoryFastingCheckInRepository()),
            CreateCurrentUserAccessService(userId),
            new FixedDateTimeProvider());

        Result<FastingInsightsModel> result = await handler.Handle(new GetFastingInsightsQuery(userId.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Contains(result.Value.Alerts, x => string.Equals(x.Id, "late", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetFastingInsights_WithRiskyCurrentCheckIn_ReturnsRiskyAlerts() {
        var userId = UserId.New();
        var current = FastingOccurrence.Create(
            FastingPlanId.New(),
            userId,
            FastingOccurrenceKind.FastDay,
            FixedNow.AddHours(-8),
            sequenceNumber: 1,
            targetHours: 36);
        var checkIn = FastingCheckIn.Create(
            current.Id,
            userId,
            hungerLevel: 3,
            energyLevel: 2,
            moodLevel: 4,
            symptoms: ["dizziness"],
            notes: "not great",
            checkedInAtUtc: FixedNow.AddHours(-1));
        var occurrenceRepo = new InMemoryFastingOccurrenceRepository(current);
        var handler = new GetFastingInsightsQueryHandler(
            occurrenceRepo,
            new FastingAnalyticsService(occurrenceRepo, new InMemoryFastingCheckInRepository(checkIn)),
            CreateCurrentUserAccessService(userId),
            new FixedDateTimeProvider());

        Result<FastingInsightsModel> result = await handler.Handle(new GetFastingInsightsQuery(userId.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Contains(result.Value.Alerts, x => string.Equals(x.Id, "current-warning", StringComparison.Ordinal));
        Assert.Contains(result.Value.Alerts, x => string.Equals(x.Id, "risky", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetFastingInsights_WithSafeCurrentCheckIn_DoesNotAddTimeBasedPrompt() {
        var userId = UserId.New();
        var current = FastingOccurrence.Create(
            FastingPlanId.New(),
            userId,
            FastingOccurrenceKind.FastDay,
            FixedNow.AddHours(-21),
            sequenceNumber: 1,
            targetHours: 36);
        var checkIn = FastingCheckIn.Create(
            current.Id,
            userId,
            hungerLevel: 2,
            energyLevel: 4,
            moodLevel: 4,
            symptoms: [],
            notes: "fine",
            checkedInAtUtc: FixedNow.AddHours(-1));
        var occurrenceRepo = new InMemoryFastingOccurrenceRepository(current);
        var handler = new GetFastingInsightsQueryHandler(
            occurrenceRepo,
            new FastingAnalyticsService(occurrenceRepo, new InMemoryFastingCheckInRepository(checkIn)),
            CreateCurrentUserAccessService(userId),
            new FixedDateTimeProvider());

        Result<FastingInsightsModel> result = await handler.Handle(new GetFastingInsightsQuery(userId.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.DoesNotContain(result.Value.Alerts, x => string.Equals(x.Id, "late", StringComparison.Ordinal));
        Assert.DoesNotContain(result.Value.Alerts, x => string.Equals(x.Id, "mid", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetFastingInsights_WithBetterShortFastsAndStrongCheckIns_ReturnsToleranceInsights() {
        var userId = UserId.New();
        var occurrenceRepo = new InMemoryFastingOccurrenceRepository();
        var checkIns = new List<FastingCheckIn>();

        void AddCompleted(int daysAgo, int targetHours, int energy, int mood, params string[] symptoms) {
            var occurrence = FastingOccurrence.Create(
                FastingPlanId.New(),
                userId,
                FastingOccurrenceKind.FastDay,
                FixedNow.AddDays(-daysAgo),
                sequenceNumber: daysAgo,
                targetHours);
            occurrence.Complete(FixedNow.AddDays(-daysAgo).AddHours(targetHours));
            occurrenceRepo.StoredOccurrences.Add(occurrence);
            checkIns.Add(FastingCheckIn.Create(
                occurrence.Id,
                userId,
                hungerLevel: 2,
                energy,
                mood,
                symptoms,
                notes: null,
                checkedInAtUtc: occurrence.StartedAtUtc.AddHours(Math.Min(targetHours, 8))));
        }

        AddCompleted(1, 16, 5, 5);
        AddCompleted(2, 16, 4, 5);
        AddCompleted(3, 18, 5, 4);
        AddCompleted(4, 36, 2, 2);
        AddCompleted(5, 36, 2, 3);

        var handler = new GetFastingInsightsQueryHandler(
            occurrenceRepo,
            new FastingAnalyticsService(occurrenceRepo, new InMemoryFastingCheckInRepository([.. checkIns])),
            CreateCurrentUserAccessService(userId),
            new FixedDateTimeProvider());

        Result<FastingInsightsModel> result = await handler.Handle(new GetFastingInsightsQuery(userId.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Contains(result.Value.Insights, x => string.Equals(x.Id, "shorter-fasts", StringComparison.Ordinal));
        Assert.Contains(result.Value.Insights, x => string.Equals(x.Id, "positive-tolerance", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetFastingInsights_WhenShorterToleranceIsNotBetter_DoesNotReturnShorterFastInsight() {
        var userId = UserId.New();
        var occurrenceRepo = new InMemoryFastingOccurrenceRepository();
        var checkIns = new List<FastingCheckIn>();

        void AddCompleted(int daysAgo, int targetHours, int energy, int mood) {
            var occurrence = FastingOccurrence.Create(
                FastingPlanId.New(),
                userId,
                FastingOccurrenceKind.FastDay,
                FixedNow.AddDays(-daysAgo),
                sequenceNumber: daysAgo,
                targetHours);
            occurrence.Complete(FixedNow.AddDays(-daysAgo).AddHours(targetHours));
            occurrenceRepo.StoredOccurrences.Add(occurrence);
            checkIns.Add(FastingCheckIn.Create(
                occurrence.Id,
                userId,
                hungerLevel: 2,
                energy,
                mood,
                symptoms: [],
                notes: null,
                checkedInAtUtc: occurrence.StartedAtUtc.AddHours(8)));
        }

        AddCompleted(1, 16, 4, 4);
        AddCompleted(2, 16, 4, 4);
        AddCompleted(3, 36, 4, 4);
        AddCompleted(4, 36, 4, 4);

        var handler = new GetFastingInsightsQueryHandler(
            occurrenceRepo,
            new FastingAnalyticsService(occurrenceRepo, new InMemoryFastingCheckInRepository([.. checkIns])),
            CreateCurrentUserAccessService(userId),
            new FixedDateTimeProvider());

        Result<FastingInsightsModel> result = await handler.Handle(new GetFastingInsightsQuery(userId.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.DoesNotContain(result.Value.Insights, x => string.Equals(x.Id, "shorter-fasts", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetCurrentFasting_WithActiveSession_ReturnsSessionWithCheckIns() {
        var userId = UserId.New();
        var plan = FastingPlan.CreateIntermittent(userId, FastingProtocol.F16_8, 16, 8, FixedNow.AddDays(-1));
        var current = FastingOccurrence.Create(plan.Id, userId, FastingOccurrenceKind.FastingWindow, FixedNow.AddHours(-4), 1, 16);
        var checkIn = FastingCheckIn.Create(current.Id, userId, 2, 4, 4, ["weakness"], "steady", FixedNow.AddHours(-1));
        var handler = new GetCurrentFastingQueryHandler(
            new InMemoryFastingOccurrenceRepository(current),
            new InMemoryFastingCheckInRepository(checkIn),
            CreateCurrentUserAccessService(userId));

        Result<FastingSessionModel?> result = await handler.Handle(new GetCurrentFastingQuery(userId.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.NotNull(result.Value);
        Assert.Equal(current.Id.Value, result.Value!.Id);
        Assert.Single(result.Value.CheckIns);
        Assert.Equal(2, result.Value.CheckIns[0].HungerLevel);
    }

    [Fact]
    public async Task GetCurrentFasting_WithMissingUserId_ReturnsInvalidToken() {
        var handler = new GetCurrentFastingQueryHandler(
            new InMemoryFastingOccurrenceRepository(),
            new InMemoryFastingCheckInRepository(),
            CreateCurrentUserAccessService(user: null));

        Result<FastingSessionModel?> result = await handler.Handle(new GetCurrentFastingQuery(UserId: null), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetCurrentFasting_WithDeletedUser_ReturnsAccountDeleted() {
        User user = CreateUser(UserId.New());
        user.DeleteAccount(FixedNow);
        var handler = new GetCurrentFastingQueryHandler(
            new InMemoryFastingOccurrenceRepository(),
            new InMemoryFastingCheckInRepository(),
            CreateCurrentUserAccessService(user));

        Result<FastingSessionModel?> result = await handler.Handle(new GetCurrentFastingQuery(user.Id.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task GetCurrentFasting_WhenNoCurrentOccurrence_ReturnsNullSuccess() {
        var userId = UserId.New();
        var handler = new GetCurrentFastingQueryHandler(
            new InMemoryFastingOccurrenceRepository(),
            new InMemoryFastingCheckInRepository(),
            CreateCurrentUserAccessService(userId));

        Result<FastingSessionModel?> result = await handler.Handle(new GetCurrentFastingQuery(userId.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task GetFastingHistory_ReturnsPagedSessionsWithCheckIns() {
        var userId = UserId.New();
        var plan = FastingPlan.CreateIntermittent(userId, FastingProtocol.F16_8, 16, 8, FixedNow.AddDays(-10));
        var latest = FastingOccurrence.Create(plan.Id, userId, FastingOccurrenceKind.FastingWindow, FixedNow.AddDays(-1), 1, 16);
        latest.Complete(FixedNow.AddDays(-1).AddHours(16));
        var earlier = FastingOccurrence.Create(plan.Id, userId, FastingOccurrenceKind.FastingWindow, FixedNow.AddDays(-2), 1, 16);
        earlier.Complete(FixedNow.AddDays(-2).AddHours(16));

        var latestCheckIn = FastingCheckIn.Create(latest.Id, userId, 3, 4, 5, ["good"], "latest", FixedNow.AddDays(-1).AddHours(8));
        var earlierCheckIn = FastingCheckIn.Create(earlier.Id, userId, 2, 3, 4, ["headache"], "earlier", FixedNow.AddDays(-2).AddHours(8));

        var occurrenceRepo = new InMemoryFastingOccurrenceRepository();
        occurrenceRepo.StoredOccurrences.AddRange([latest, earlier]);
        var handler = new GetFastingHistoryQueryHandler(
            new FastingAnalyticsService(
                occurrenceRepo,
                new InMemoryFastingCheckInRepository(latestCheckIn, earlierCheckIn)),
            CreateCurrentUserAccessService(userId));

        Result<PagedResponse<FastingSessionModel>> result = await handler.Handle(
            new GetFastingHistoryQuery(userId.Value, FixedNow.AddDays(-7), FixedNow, 1, 1),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Single(result.Value.Data);
        Assert.Equal(2, result.Value.TotalItems);
        Assert.Equal(2, result.Value.TotalPages);
        Assert.Single(result.Value.Data[0].CheckIns);
        Assert.Equal("latest", result.Value.Data[0].CheckIns[0].Notes);
    }

    [Fact]
    public async Task GetFastingHistory_WhenNoOccurrences_ReturnsEmptyPage() {
        var userId = UserId.New();
        var handler = new GetFastingHistoryQueryHandler(
            new FastingAnalyticsService(
                new InMemoryFastingOccurrenceRepository(),
                new InMemoryFastingCheckInRepository()),
            CreateCurrentUserAccessService(userId));

        Result<PagedResponse<FastingSessionModel>> result = await handler.Handle(
            new GetFastingHistoryQuery(userId.Value, FixedNow.AddDays(-7), FixedNow, 1, 10),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Empty(result.Value.Data);
        Assert.Equal(0, result.Value.TotalItems);
        Assert.Equal(0, result.Value.TotalPages);
    }

    [Fact]
    public async Task GetFastingHistory_WithUnspecifiedDateRange_NormalizesDatesToUtc() {
        var userId = UserId.New();
        var analytics = new RecordingFastingAnalyticsService();
        var handler = new GetFastingHistoryQueryHandler(analytics, CreateCurrentUserAccessService(userId));
        var from = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Unspecified);
        var to = new DateTime(2026, 4, 30, 23, 59, 59, DateTimeKind.Unspecified);

        Result<PagedResponse<FastingSessionModel>> result = await handler.Handle(new GetFastingHistoryQuery(userId.Value, from, to, 1, 10), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(DateTimeKind.Utc, analytics.FromUtc.Kind);
        Assert.Equal(DateTimeKind.Utc, analytics.ToUtc.Kind);
        Assert.Equal(DateTime.SpecifyKind(from, DateTimeKind.Utc), analytics.FromUtc);
        Assert.Equal(DateTime.SpecifyKind(to, DateTimeKind.Utc), analytics.ToUtc);
    }

    [Fact]
    public async Task GetFastingHistory_WithLocalDateRange_ConvertsDatesToUtc() {
        var userId = UserId.New();
        var analytics = new RecordingFastingAnalyticsService();
        var handler = new GetFastingHistoryQueryHandler(analytics, CreateCurrentUserAccessService(userId));
        var from = new DateTime(2026, 4, 1, 4, 0, 0, DateTimeKind.Local);
        var to = new DateTime(2026, 4, 30, 23, 59, 59, DateTimeKind.Local);

        Result<PagedResponse<FastingSessionModel>> result = await handler.Handle(new GetFastingHistoryQuery(userId.Value, from, to, 1, 10), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(DateTimeKind.Utc, analytics.FromUtc.Kind);
        Assert.Equal(DateTimeKind.Utc, analytics.ToUtc.Kind);
        Assert.Equal(from.ToUniversalTime(), analytics.FromUtc);
        Assert.Equal(to.ToUniversalTime(), analytics.ToUtc);
    }

    [Fact]
    public async Task GetFastingHistory_WithMissingUserId_ReturnsInvalidToken() {
        var handler = new GetFastingHistoryQueryHandler(new RecordingFastingAnalyticsService(), CreateCurrentUserAccessService(user: null));

        Result<PagedResponse<FastingSessionModel>> result = await handler.Handle(new GetFastingHistoryQuery(UserId: null, FixedNow.AddDays(-7), FixedNow, 1, 10), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetFastingHistory_WithDeletedUser_ReturnsAccountDeleted() {
        User user = CreateUser(UserId.New());
        user.DeleteAccount(FixedNow);
        var handler = new GetFastingHistoryQueryHandler(new RecordingFastingAnalyticsService(), CreateCurrentUserAccessService(user));

        Result<PagedResponse<FastingSessionModel>> result = await handler.Handle(new GetFastingHistoryQuery(user.Id.Value, FixedNow.AddDays(-7), FixedNow, 1, 10), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task GetFastingStats_ComputesRatesAndTopSymptom() {
        var userId = UserId.New();
        DateTime now = FixedNow;

        var oldCompleted = FastingOccurrence.Create(FastingPlanId.New(), userId, FastingOccurrenceKind.FastingWindow, now.AddDays(-40), 1, 16);
        oldCompleted.Complete(now.AddDays(-40).AddHours(16));

        var completedOne = FastingOccurrence.Create(FastingPlanId.New(), userId, FastingOccurrenceKind.FastingWindow, now.AddDays(-2), 1, 16);
        completedOne.Complete(now.AddDays(-2).AddHours(16));

        var completedTwo = FastingOccurrence.Create(FastingPlanId.New(), userId, FastingOccurrenceKind.FastingWindow, now.AddDays(-1), 1, 16);
        completedTwo.Complete(now.AddDays(-1).AddHours(16));

        var active = FastingOccurrence.Create(FastingPlanId.New(), userId, FastingOccurrenceKind.FastingWindow, now, 1, 16);
        var oldCheckIn = FastingCheckIn.Create(oldCompleted.Id, userId, 3, 4, 4, ["weakness"], "old", now.AddDays(-40).AddHours(8));
        var completedOneCheckIn = FastingCheckIn.Create(completedOne.Id, userId, 2, 4, 4, ["dizziness"], "one", now.AddDays(-2).AddHours(8));
        var completedTwoCheckIn = FastingCheckIn.Create(completedTwo.Id, userId, 3, 5, 5, ["dizziness"], "two", now.AddDays(-1).AddHours(8));

        var occurrenceRepo = new InMemoryFastingOccurrenceRepository();
        occurrenceRepo.StoredOccurrences.AddRange([oldCompleted, completedOne, completedTwo, active]);
        var handler = new GetFastingStatsQueryHandler(
            new FastingAnalyticsService(
                occurrenceRepo,
                new InMemoryFastingCheckInRepository(oldCheckIn, completedOneCheckIn, completedTwoCheckIn)),
            CreateCurrentUserAccessService(userId),
            new FixedDateTimeProvider());

        Result<FastingStatsModel> result = await handler.Handle(new GetFastingStatsQuery(userId.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(3, result.Value.TotalCompleted);
        Assert.Equal(2, result.Value.CurrentStreak);
        Assert.Equal(66.7, result.Value.CompletionRateLast30Days);
        Assert.Equal(66.7, result.Value.CheckInRateLast30Days);
        Assert.Equal("dizziness", result.Value.TopSymptom);
        Assert.Equal(now.AddDays(-1).AddHours(8), result.Value.LastCheckInAtUtc);
    }

    [Fact]
    public async Task GetFastingStats_WhenNoHistory_ReturnsZeroMetrics() {
        var userId = UserId.New();
        var occurrenceRepo = new InMemoryFastingOccurrenceRepository();
        var handler = new GetFastingStatsQueryHandler(
            new FastingAnalyticsService(occurrenceRepo, new InMemoryFastingCheckInRepository()),
            CreateCurrentUserAccessService(userId),
            new FixedDateTimeProvider());

        Result<FastingStatsModel> result = await handler.Handle(new GetFastingStatsQuery(userId.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(0, result.Value.TotalCompleted);
        Assert.Equal(0, result.Value.CurrentStreak);
        Assert.Equal(0, result.Value.AverageDurationHours);
        Assert.Equal(0, result.Value.CompletionRateLast30Days);
        Assert.Equal(0, result.Value.CheckInRateLast30Days);
        Assert.Null(result.Value.LastCheckInAtUtc);
        Assert.Null(result.Value.TopSymptom);
    }

    [Fact]
    public async Task GetFastingStats_WithMissingUserId_ReturnsInvalidToken() {
        var handler = new GetFastingStatsQueryHandler(
            new RecordingFastingAnalyticsService(),
            CreateCurrentUserAccessService(user: null),
            new FixedDateTimeProvider());

        Result<FastingStatsModel> result = await handler.Handle(new GetFastingStatsQuery(UserId: null), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetFastingStats_WithDeletedUser_ReturnsAccountDeleted() {
        User user = CreateUser(UserId.New());
        user.DeleteAccount(FixedNow);
        var handler = new GetFastingStatsQueryHandler(
            new RecordingFastingAnalyticsService(),
            CreateCurrentUserAccessService(user),
            new FixedDateTimeProvider());

        Result<FastingStatsModel> result = await handler.Handle(new GetFastingStatsQuery(user.Id.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task GetFastingOverview_ReturnsCurrentStatsInsightsAndHistory() {
        var userId = UserId.New();
        var plan = FastingPlan.CreateIntermittent(userId, FastingProtocol.F16_8, 16, 8, FixedNow.AddDays(-5));
        var current = FastingOccurrence.Create(plan.Id, userId, FastingOccurrenceKind.FastingWindow, FixedNow.AddHours(-13), 1, 16);
        current.UpdateCheckIn(2, 2, 2, ["weakness"], "current", FixedNow.AddHours(-1));
        var currentCheckIn = FastingCheckIn.Create(current.Id, userId, 2, 2, 2, ["weakness"], "current", FixedNow.AddHours(-1));

        var historyOne = FastingOccurrence.Create(plan.Id, userId, FastingOccurrenceKind.FastingWindow, FixedNow.AddDays(-3), 1, 16);
        historyOne.UpdateCheckIn(5, 5, 5, ["headache"], "history-one", FixedNow.AddDays(-3).AddHours(8));
        historyOne.Complete(FixedNow.AddDays(-3).AddHours(16));

        var historyTwo = FastingOccurrence.Create(plan.Id, userId, FastingOccurrenceKind.FastingWindow, FixedNow.AddDays(-2), 1, 16);
        historyTwo.UpdateCheckIn(4, 4, 4, ["headache"], "history-two", FixedNow.AddDays(-2).AddHours(8));
        historyTwo.Complete(FixedNow.AddDays(-2).AddHours(16));

        var occurrenceRepo = new InMemoryFastingOccurrenceRepository(current);
        occurrenceRepo.StoredOccurrences.InsertRange(0, [historyOne, historyTwo]);
        var checkInRepo = new InMemoryFastingCheckInRepository(currentCheckIn);
        var handler = new GetFastingOverviewQueryHandler(
            occurrenceRepo,
            checkInRepo,
            new FastingAnalyticsService(occurrenceRepo, checkInRepo),
            CreateCurrentUserAccessService(userId),
            new FixedDateTimeProvider());

        Result<FastingOverviewModel> result = await handler.Handle(new GetFastingOverviewQuery(userId.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.NotNull(result.Value.CurrentSession);
        Assert.Single(result.Value.CurrentSession!.CheckIns);
        Assert.Equal(2, result.Value.Stats.TotalCompleted);
        Assert.Contains(result.Value.Insights.Alerts, x => string.Equals(x.Id, "current-warning", StringComparison.Ordinal));
        Assert.Contains(result.Value.Insights.Insights, x => string.Equals(x.Id, "symptom-headache", StringComparison.Ordinal));
        Assert.Equal(1, result.Value.History.Page);
        Assert.True(result.Value.History.Data.Count >= 3);
    }

    [Fact]
    public async Task GetFastingOverview_WithDeletedUser_ReturnsAccountDeleted() {
        User user = CreateUser(UserId.New());
        user.DeleteAccount(FixedNow);
        var occurrenceRepo = new InMemoryFastingOccurrenceRepository();
        var checkInRepo = new InMemoryFastingCheckInRepository();
        var handler = new GetFastingOverviewQueryHandler(
            occurrenceRepo,
            checkInRepo,
            new FastingAnalyticsService(occurrenceRepo, checkInRepo),
            CreateCurrentUserAccessService(user),
            new FixedDateTimeProvider());

        Result<FastingOverviewModel> result = await handler.Handle(new GetFastingOverviewQuery(user.Id.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task GetFastingOverview_WithMissingUserId_ReturnsInvalidToken() {
        var occurrenceRepo = new InMemoryFastingOccurrenceRepository();
        var checkInRepo = new InMemoryFastingCheckInRepository();
        var handler = new GetFastingOverviewQueryHandler(
            occurrenceRepo,
            checkInRepo,
            new FastingAnalyticsService(occurrenceRepo, checkInRepo),
            CreateCurrentUserAccessService(user: null),
            new FixedDateTimeProvider());

        Result<FastingOverviewModel> result = await handler.Handle(new GetFastingOverviewQuery(UserId: null), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetFastingInsights_WithMissingUserId_ReturnsInvalidToken() {
        var occurrenceRepo = new InMemoryFastingOccurrenceRepository();
        var checkInRepo = new InMemoryFastingCheckInRepository();
        var handler = new GetFastingInsightsQueryHandler(
            occurrenceRepo,
            new FastingAnalyticsService(occurrenceRepo, checkInRepo),
            CreateCurrentUserAccessService(user: null),
            new FixedDateTimeProvider());

        Result<FastingInsightsModel> result = await handler.Handle(new GetFastingInsightsQuery(UserId: null), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetFastingInsights_WithDeletedUser_ReturnsAccountDeleted() {
        User user = CreateUser(UserId.New());
        user.DeleteAccount(FixedNow);
        var occurrenceRepo = new InMemoryFastingOccurrenceRepository();
        var checkInRepo = new InMemoryFastingCheckInRepository();
        var handler = new GetFastingInsightsQueryHandler(
            occurrenceRepo,
            new FastingAnalyticsService(occurrenceRepo, checkInRepo),
            CreateCurrentUserAccessService(user),
            new FixedDateTimeProvider());

        Result<FastingInsightsModel> result = await handler.Handle(new GetFastingInsightsQuery(user.Id.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public void GetDefaultHistoryWindow_UsesCanonicalUtcMonthWindow() {
        var service = new FastingAnalyticsService(new InMemoryFastingOccurrenceRepository(), new InMemoryFastingCheckInRepository());
        (DateTime fromUtc, DateTime toUtc) = service.GetDefaultHistoryWindow(new DateTime(2026, 1, 15, 9, 30, 0, DateTimeKind.Utc));

        Assert.Equal(new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc), fromUtc);
        Assert.Equal(new DateTime(2026, 2, 28, 23, 59, 59, 999, DateTimeKind.Utc).AddTicks(9999), toUtc);
    }
}
