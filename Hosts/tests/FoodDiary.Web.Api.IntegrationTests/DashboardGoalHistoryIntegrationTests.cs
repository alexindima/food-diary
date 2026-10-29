using System.Net.Http.Json;
using System.Text.Json;
using FoodDiary.Domain.Primitives;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Requests;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class DashboardGoalHistoryIntegrationTests(PostgresApiWebApplicationFactory factory) : IClassFixture<PostgresApiWebApplicationFactory> {
    [RequiresDockerFact]
    public async Task Dashboard_HistoricalGoalsSurviveLaterChanges_ThroughTheRealSnapshotComposition() {
        using HttpClient client = factory.CreateClient();
        string email = $"goal-history-{Guid.NewGuid():N}@example.com";
        using HttpResponseMessage registration = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterHttpRequest(email, "Password123!", "en"));
        registration.EnsureSuccessStatusCode();
        AuthenticationResponseCookies.ApplyBrowserSession(client, AuthenticationResponseCookies.ReadBrowserSession(registration));
        await using (AsyncServiceScope scope = factory.Services.CreateAsyncScope()) {
            FoodDiaryDbContext context = scope.ServiceProvider.GetRequiredService<FoodDiaryDbContext>();
            User user = await context.Users.SingleAsync(account => account.Email == email);
            user.SetTimeZone("Asia/Tbilisi");
            using (DomainTime.Override(new FixedTimeProvider(new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero)))) {
                user.UpdateGoals(dailyCalorieTarget: 3000, proteinTarget: 150, waterGoal: 2000);
                await context.SaveChangesAsync();
            }
            using (DomainTime.Override(new FixedTimeProvider(new DateTimeOffset(2026, 10, 29, 22, 0, 0, TimeSpan.Zero)))) {
                user.UpdateGoals(dailyCalorieTarget: 2500, proteinTarget: 120, waterGoal: 2500);
                await context.SaveChangesAsync();
            }
        }

        using HttpResponseMessage oldResponse = await client.GetAsync("/api/v1/dashboard?date=2026-10-29&timeZoneId=Asia%2FTbilisi&locale=en");
        oldResponse.EnsureSuccessStatusCode();
        using var oldPayload = JsonDocument.Parse(await oldResponse.Content.ReadAsStringAsync());
        using HttpResponseMessage newResponse = await client.GetAsync("/api/v1/dashboard?date=2026-10-30&timeZoneId=Asia%2FTbilisi&locale=en");
        newResponse.EnsureSuccessStatusCode();
        using var newPayload = JsonDocument.Parse(await newResponse.Content.ReadAsStringAsync());
        Assert.Multiple(
            () => Assert.Equal(3000, oldPayload.RootElement.GetProperty("dailyGoal").GetDouble()),
            () => Assert.Equal(150, oldPayload.RootElement.GetProperty("statistics").GetProperty("proteinGoal").GetDouble()),
            () => Assert.Equal(2000, oldPayload.RootElement.GetProperty("hydration").GetProperty("goalMl").GetDouble()),
            () => Assert.Equal(2500, newPayload.RootElement.GetProperty("dailyGoal").GetDouble()),
            () => Assert.Equal(120, newPayload.RootElement.GetProperty("statistics").GetProperty("proteinGoal").GetDouble()),
            () => Assert.Equal(2500, newPayload.RootElement.GetProperty("hydration").GetProperty("goalMl").GetDouble()),
            () => Assert.Equal(20500, newPayload.RootElement.GetProperty("weeklyCalorieGoal").GetDouble()));
    }

    [ExcludeFromCodeCoverage]
    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
