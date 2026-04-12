using System.Net;
using System.Net.Http.Json;
using FoodDiary.Domain.Entities.Tracking.Fasting;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Presentation.Api.Features.Fasting.Requests;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Web.Api.IntegrationTests;

public sealed class FastingApiIntegrationTests(TestAuthApiWebApplicationFactory factory)
    : IClassFixture<TestAuthApiWebApplicationFactory> {
    [Fact]
    public async Task GetOverview_WithActiveSession_ReturnsCurrentStatsAlertsAndHistory() {
        var user = await SeedUserAsync();
        var plan = FastingPlan.CreateIntermittent(user.Id, FastingProtocol.F16_8, 16, 8, DateTime.UtcNow.AddDays(-5));
        var completed = FastingOccurrence.Create(plan.Id, user.Id, FastingOccurrenceKind.FastingWindow, DateTime.UtcNow.AddDays(-2), 1, 16);
        completed.UpdateCheckIn(4, 4, 4, ["headache"], "completed", DateTime.UtcNow.AddDays(-2).AddHours(8));
        completed.Complete(DateTime.UtcNow.AddDays(-2).AddHours(16));
        var current = FastingOccurrence.Create(plan.Id, user.Id, FastingOccurrenceKind.FastingWindow, DateTime.UtcNow.AddHours(-13), 2, 16);

        await SeedFastingDataAsync(plan, completed, current);

        var client = CreateAuthenticatedClient(user);
        var response = await client.GetAsync("/api/v1/fasting/overview");
        var payload = await response.Content.ReadFromJsonAsync<FastingOverviewPayload>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.NotNull(payload.CurrentSession);
        Assert.Equal(current.Id.Value, payload.CurrentSession!.Id);
        Assert.Equal(1, payload.Stats.TotalCompleted);
        Assert.Contains(payload.Insights.Alerts, x => x.Id == "mid");
        Assert.NotEmpty(payload.History.Data);
    }

    [Fact]
    public async Task UpdateCheckIn_PersistsCheckIn_AndCurrentReturnsIt() {
        var user = await SeedUserAsync();
        var plan = FastingPlan.CreateIntermittent(user.Id, FastingProtocol.F16_8, 16, 8, DateTime.UtcNow.AddHours(-6));
        var current = FastingOccurrence.Create(plan.Id, user.Id, FastingOccurrenceKind.FastingWindow, DateTime.UtcNow.AddHours(-6), 1, 16);
        await SeedFastingDataAsync(plan, current);

        var client = CreateAuthenticatedClient(user);
        var updateResponse = await client.PutAsJsonAsync(
            "/api/v1/fasting/current/check-in",
            new UpdateFastingCheckInHttpRequest(2, 4, 4, ["weakness"], "holding steady"));
        var updated = await updateResponse.Content.ReadFromJsonAsync<FastingSessionPayload>();

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.NotNull(updated);
        Assert.NotNull(updated.CheckInAtUtc);
        Assert.Single(updated.CheckIns);
        Assert.Equal("holding steady", updated.CheckIns[0].Notes);

        var currentResponse = await client.GetAsync("/api/v1/fasting/current");
        var currentPayload = await currentResponse.Content.ReadFromJsonAsync<FastingSessionPayload>();

        Assert.Equal(HttpStatusCode.OK, currentResponse.StatusCode);
        Assert.NotNull(currentPayload);
        Assert.Single(currentPayload.CheckIns);
        Assert.Equal("weakness", Assert.Single(currentPayload.CheckIns[0].Symptoms));

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FoodDiaryDbContext>();
        Assert.Single(dbContext.FastingCheckIns.Where(x => x.UserId == user.Id));
    }

    [Fact]
    public async Task GetHistory_ReturnsPagedSessionsWithCheckIns() {
        var user = await SeedUserAsync();
        var plan = FastingPlan.CreateIntermittent(user.Id, FastingProtocol.F16_8, 16, 8, DateTime.UtcNow.AddDays(-10));

        var latest = FastingOccurrence.Create(plan.Id, user.Id, FastingOccurrenceKind.FastingWindow, DateTime.UtcNow.AddDays(-1), 1, 16);
        latest.UpdateCheckIn(3, 4, 5, ["good"], "latest", DateTime.UtcNow.AddDays(-1).AddHours(8));
        latest.Complete(DateTime.UtcNow.AddDays(-1).AddHours(16));

        var earlier = FastingOccurrence.Create(plan.Id, user.Id, FastingOccurrenceKind.FastingWindow, DateTime.UtcNow.AddDays(-2), 2, 16);
        earlier.UpdateCheckIn(2, 3, 4, ["headache"], "earlier", DateTime.UtcNow.AddDays(-2).AddHours(8));
        earlier.Complete(DateTime.UtcNow.AddDays(-2).AddHours(16));

        var latestCheckIn = FastingCheckIn.Create(latest.Id, user.Id, 3, 4, 5, ["good"], "latest", DateTime.UtcNow.AddDays(-1).AddHours(8));
        var earlierCheckIn = FastingCheckIn.Create(earlier.Id, user.Id, 2, 3, 4, ["headache"], "earlier", DateTime.UtcNow.AddDays(-2).AddHours(8));
        await SeedFastingDataAsync(plan, latest, earlier);
        await SeedCheckInsAsync(latestCheckIn, earlierCheckIn);

        var client = CreateAuthenticatedClient(user);
        var from = Uri.EscapeDataString(DateTime.UtcNow.AddDays(-7).ToString("O"));
        var to = Uri.EscapeDataString(DateTime.UtcNow.ToString("O"));
        var response = await client.GetAsync($"/api/v1/fasting/history?from={from}&to={to}&page=1&limit=1");
        var payload = await response.Content.ReadFromJsonAsync<PagedPayload<FastingSessionPayload>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Single(payload.Data);
        Assert.Equal(2, payload.TotalItems);
        Assert.Equal(2, payload.TotalPages);
        Assert.Single(payload.Data[0].CheckIns);
    }

    private HttpClient CreateAuthenticatedClient(User user) {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.AuthenticateHeader, "true");
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, user.Id.Value.ToString());
        return client;
    }

    private async Task<User> SeedUserAsync() {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FoodDiaryDbContext>();
        var user = User.Create($"fasting-api-{Guid.NewGuid():N}@example.com", "hash");
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        return user;
    }

    private async Task SeedFastingDataAsync(FastingPlan plan, params FastingOccurrence[] occurrences) {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FoodDiaryDbContext>();
        dbContext.FastingPlans.Add(plan);
        dbContext.FastingOccurrences.AddRange(occurrences);
        await dbContext.SaveChangesAsync();
    }

    private async Task SeedCheckInsAsync(params FastingCheckIn[] checkIns) {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FoodDiaryDbContext>();
        dbContext.FastingCheckIns.AddRange(checkIns);
        await dbContext.SaveChangesAsync();
    }

    private sealed record FastingOverviewPayload(
        FastingSessionPayload? CurrentSession,
        FastingStatsPayload Stats,
        FastingInsightsPayload Insights,
        PagedPayload<FastingSessionPayload> History);

    private sealed record FastingStatsPayload(
        int TotalCompleted,
        int CurrentStreak,
        double AverageDurationHours,
        double CompletionRateLast30Days,
        double CheckInRateLast30Days,
        DateTime? LastCheckInAtUtc,
        string? TopSymptom);

    private sealed record FastingInsightsPayload(
        IReadOnlyList<FastingMessagePayload> Alerts,
        IReadOnlyList<FastingMessagePayload> Insights);

    private sealed record FastingMessagePayload(
        string Id,
        string TitleKey,
        string BodyKey,
        string Severity,
        IReadOnlyDictionary<string, string>? Parameters);

    private sealed record PagedPayload<T>(
        IReadOnlyList<T> Data,
        int Page,
        int Limit,
        int TotalPages,
        int TotalItems);

    private sealed record FastingSessionPayload(
        Guid Id,
        DateTime StartedAtUtc,
        DateTime? EndedAtUtc,
        int InitialPlannedDurationHours,
        int AddedDurationHours,
        int PlannedDurationHours,
        string Protocol,
        string PlanType,
        string OccurrenceKind,
        int? CyclicFastDays,
        int? CyclicEatDays,
        int? CyclicEatDayFastHours,
        int? CyclicEatDayEatingWindowHours,
        int? CyclicPhaseDayNumber,
        int? CyclicPhaseDayTotal,
        bool IsCompleted,
        string Status,
        string? Notes,
        DateTime? CheckInAtUtc,
        int? HungerLevel,
        int? EnergyLevel,
        int? MoodLevel,
        IReadOnlyList<string> Symptoms,
        string? CheckInNotes,
        IReadOnlyList<FastingCheckInPayload> CheckIns);

    private sealed record FastingCheckInPayload(
        Guid Id,
        DateTime CheckedInAtUtc,
        int HungerLevel,
        int EnergyLevel,
        int MoodLevel,
        IReadOnlyList<string> Symptoms,
        string? Notes);
}
