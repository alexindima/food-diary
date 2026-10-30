using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Cycles.Domain.Contracts.Enums;
using FoodDiary.Modules.Cycles.Presentation.Requests;
using FoodDiary.Modules.Fasting.Domain.Entities.Tracking.Fasting;
using FoodDiary.Modules.Fasting.Domain.Enums;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Requests;
using FoodDiary.Modules.Meals.Presentation.Requests;
using FoodDiary.Modules.Products.Presentation.Requests;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class DashboardQueryBudgetIntegrationTests(PostgresApiWebApplicationFactory factory, ITestOutputHelper output)
    : IClassFixture<PostgresApiWebApplicationFactory> {
    [RequiresDockerTheory]
    [InlineData(false, false, 1, false, "en")]
    [InlineData(true, false, 1, false, "en")]
    [InlineData(true, true, 1, false, "en")]
    [InlineData(true, true, 10, false, "en")]
    [InlineData(true, true, 10, true, "en")]
    [InlineData(true, true, 11, true, "ru")]
    public async Task Dashboard_AfterCreatingAMeal_StaysWithinTheSnapshotQueryBudget(
        bool includeCycle, bool includeAiSession, int mealCount, bool includeFasting, string locale) {
        IHttpContextAccessor httpContext = factory.Services.GetRequiredService<IHttpContextAccessor>();
        string requestMarker = Guid.NewGuid().ToString("N");
        int snapshotQueries = 0;
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) => {
            if (string.Equals(instrument.Name, "fooddiary.read_snapshot.queries", StringComparison.Ordinal)) {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<int>((_, count, _, _) => {
            if (string.Equals(httpContext.HttpContext?.Request.Headers["X-Dashboard-Budget-Test"].ToString(), requestMarker, StringComparison.Ordinal)) {
                Volatile.Write(ref snapshotQueries, count);
            }
        });
        listener.Start();
        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dashboard-Budget-Test", requestMarker);
        string email = $"dashboard-budget-{Guid.NewGuid():N}@example.com";
        using HttpResponseMessage registration = await client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterHttpRequest(email, "Password123!", "en"));
        registration.EnsureSuccessStatusCode();
        AuthenticationResponseCookies.ApplyBrowserSession(client, AuthenticationResponseCookies.ReadBrowserSession(registration));
        if (includeCycle) {
            using HttpResponseMessage cycle = await client.PostAsJsonAsync("/api/v1/cycles", new CreateCycleHttpRequest(
                new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc), (int)CycleTrackingMode.PeriodTracking,
                AverageCycleLength: 28, AveragePeriodLength: 5, LutealLength: 14, IsRegular: true,
                IsOnboardingComplete: true, ShowFertilityEstimates: false, DiscreetNotifications: true,
                Notes: null, CycleTrackingConsentGranted: true));
            cycle.EnsureSuccessStatusCode();
        }
        if (includeFasting) {
            await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();
            FoodDiaryDbContext db = scope.ServiceProvider.GetRequiredService<FoodDiaryDbContext>();
            UserId userId = await db.Users.Where(user => user.Email == email).Select(user => user.Id).SingleAsync();
            var startedAt = new DateTime(2026, 10, 30, 6, 0, 0, DateTimeKind.Utc);
            var plan = FastingPlan.CreateIntermittent(userId, FastingProtocol.Fast16Eat8, 16, 8, startedAt);
            var occurrence = FastingOccurrence.Create(plan.Id, userId, FastingOccurrenceKind.FastingWindow, startedAt, 1, 16);
            db.FastingPlans.Add(plan);
            db.FastingOccurrences.Add(occurrence);
            await db.SaveChangesAsync();
            if (string.Equals(locale, "ru", StringComparison.Ordinal)) {
                // Force the supported-locale fallback, independently of seeded advice translations.
                await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM \"DailyAdvices\" WHERE \"Locale\" = {locale}");
            }
        }

        string dashboardUrl = $"/api/v1/dashboard?date=2026-10-30T00:00:00.000Z&locale={locale}&trendDays=30&timeZoneOffsetMinutes=180&timeZoneId=Europe%2FMoscow";
        using HttpResponseMessage empty = await client.GetAsync(dashboardUrl + "&limit=1");
        output.WriteLine(FormattableString.Invariant($"Empty dashboard: {snapshotQueries} snapshot queries."));
        Assert.True(empty.StatusCode == HttpStatusCode.OK,
            FormattableString.Invariant($"Empty dashboard must return 200 within 32 snapshot queries. Observed {empty.StatusCode}, {snapshotQueries} queries."));

        using HttpResponseMessage product = await client.PostAsJsonAsync("/api/v1/products", new CreateProductHttpRequest(
            Barcode: null, Name: "Dashboard budget soup", Brand: null, ProductType: "Unknown", Category: null,
            Description: null, Comment: null, ImageUrl: null, ImageAssetId: null, BaseUnit: "G", BaseAmount: 100,
            DefaultPortionAmount: 100, CaloriesPerBase: 500, ProteinsPerBase: 30, FatsPerBase: 20,
            CarbsPerBase: 50, FiberPerBase: 5, AlcoholPerBase: 0, Visibility: "Private"));
        product.EnsureSuccessStatusCode();
        using var productPayload = JsonDocument.Parse(await product.Content.ReadAsStringAsync());
        Guid productId = productPayload.RootElement.GetProperty("id").GetGuid();
        var mealDate = new DateTime(2026, 10, 30, 9, 0, 0, DateTimeKind.Utc);
        for (int index = 0; index < mealCount; index++) {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/meals") {
                Content = JsonContent.Create(new CreateMealHttpRequest(
                    mealDate.AddMinutes(index), "Lunch", Comment: null, ImageUrl: null, ImageAssetId: null,
                    Items: [new MealItemHttpRequest(productId, RecipeId: null, Amount: 100)],
                    AiSessions: includeAiSession ? [new MealAiSessionHttpRequest(
                        ImageAssetId: null, Source: "Text", RecognizedAtUtc: mealDate, Notes: null,
                        Items: [new MealAiItemHttpRequest("Soup", NameLocal: null, Amount: 200, Unit: "G",
                            Calories: 500, Proteins: 30, Fats: 20, Carbs: 50, Fiber: 5, Alcohol: 0)])] : null,
                    IsNutritionAutoCalculated: false, ManualCalories: 500, ManualProteins: 30,
                    ManualFats: 20, ManualCarbs: 50, ManualFiber: 5, ManualAlcohol: 0)),
            };
            request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("D"));
            using HttpResponseMessage created = await client.SendAsync(request);
            Assert.True(created.StatusCode == HttpStatusCode.Created, await created.Content.ReadAsStringAsync());
        }

        Volatile.Write(ref snapshotQueries, 0);
        using HttpResponseMessage response = await client.GetAsync(dashboardUrl + "&page=1&limit=10");
        output.WriteLine(FormattableString.Invariant($"Populated dashboard: {snapshotQueries} snapshot queries."));
        Assert.True(response.StatusCode == HttpStatusCode.OK,
            FormattableString.Invariant($"Dashboard after meal creation must return 200 within 32 snapshot queries. Observed {response.StatusCode}, {snapshotQueries} queries. {await response.Content.ReadAsStringAsync()}"));
        Assert.InRange(snapshotQueries, 1, 32);
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Multiple(
            () => Assert.Equal(mealCount, payload.RootElement.GetProperty("meals").GetProperty("total").GetInt32()),
            () => Assert.Equal(500 * mealCount, payload.RootElement.GetProperty("statistics").GetProperty("totalCalories").GetDouble()),
            () => Assert.Equal(JsonValueKind.Object, payload.RootElement.GetProperty("tdeeInsight").ValueKind),
            () => Assert.Equal(includeCycle ? JsonValueKind.Object : JsonValueKind.Null, payload.RootElement.GetProperty("currentCycle").ValueKind),
            () => Assert.Equal(includeFasting ? JsonValueKind.Object : JsonValueKind.Null, payload.RootElement.GetProperty("currentFastingSession").ValueKind));
        JsonElement[] meals = [.. payload.RootElement.GetProperty("meals").GetProperty("items").EnumerateArray()];
        Assert.Equal(Math.Min(mealCount, 10), meals.Length);
        Assert.All(meals, meal => {
            Assert.Single(meal.GetProperty("items").EnumerateArray());
            JsonElement[] sessions = [.. meal.GetProperty("aiSessions").EnumerateArray()];
            Assert.Equal(includeAiSession ? 1 : 0, sessions.Length);
            if (includeAiSession) {
                JsonElement item = Assert.Single(Assert.Single(sessions).GetProperty("items").EnumerateArray());
                Assert.Equal("Soup", item.GetProperty("nameEn").GetString());
            }
        });
        if (mealCount > 10) {
            foreach (int page in new[] { 2, 3 }) {
                Volatile.Write(ref snapshotQueries, 0);
                using HttpResponseMessage paged = await client.GetAsync(dashboardUrl + FormattableString.Invariant($"&page={page}&limit=10"));
                Assert.True(paged.StatusCode == HttpStatusCode.OK, await paged.Content.ReadAsStringAsync());
                Assert.InRange(snapshotQueries, 1, 32);
                using var pagePayload = JsonDocument.Parse(await paged.Content.ReadAsStringAsync());
                Assert.Equal(mealCount, pagePayload.RootElement.GetProperty("meals").GetProperty("total").GetInt32());
                Assert.Equal(page == 2 ? 1 : 0, pagePayload.RootElement.GetProperty("meals").GetProperty("items").GetArrayLength());
            }
        }
    }
}
