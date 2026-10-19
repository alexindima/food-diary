using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FoodDiary.Modules.Cycles.Domain.Contracts.Enums;
using FoodDiary.Modules.Cycles.Presentation.Requests;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Requests;
using FoodDiary.Modules.Meals.Presentation.Requests;
using FoodDiary.Modules.Products.Presentation.Requests;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class CycleNutritionCalendarContractTests(TestAuthApiWebApplicationFactory factory)
    : IClassFixture<TestAuthApiWebApplicationFactory> {
    [RequiresDockerTheory]
    [InlineData("Asia/Tbilisi", "2026-08-28T20:00:00Z", "2026-08-29T20:00:00Z", "2026-08-30T20:00:00Z")]
    [InlineData("America/Los_Angeles", "2026-08-29T07:00:00Z", "2026-08-30T07:00:00Z", "2026-08-31T07:00:00Z")]
    [InlineData("Asia/Kathmandu", "2026-08-28T18:15:00Z", "2026-08-29T18:15:00Z", "2026-08-30T18:15:00Z")]
    public async Task NutritionSummary_GroupsLocalMidnightMealsAndExcludesOutsideRange(
        string zoneId, string fromInstant, string secondInstant, string untilInstant) {
        using HttpClient client = factory.CreateClient();
        await AuthenticateNewUserAsync(client);
        Guid profileId = await CreateCycleAsync(client);
        Guid productId = await CreateProductAsync(client);
        HttpResponseMessage day = await client.PutAsJsonAsync($"/api/v1/cycles/{profileId}/days", new UpsertCycleDayHttpRequest(
            new DateTime(2026, 8, 29, 0, 0, 0, DateTimeKind.Utc), new BleedingLogHttpModel(0, 2, 0, Notes: null, ClearNotes: false),
            [], FertilitySignal: null));
        day.EnsureSuccessStatusCode();
        HttpResponseMessage confirmed = await client.PutAsJsonAsync($"/api/v1/cycles/{profileId}/period-start",
            new ConfirmPeriodStartHttpRequest(new DateTime(2026, 9, 26, 0, 0, 0, DateTimeKind.Utc)));
        confirmed.EnsureSuccessStatusCode();
        DateTime from = DateTimeOffset.Parse(fromInstant, CultureInfo.InvariantCulture).UtcDateTime;
        DateTime second = DateTimeOffset.Parse(secondInstant, CultureInfo.InvariantCulture).UtcDateTime;
        DateTime until = DateTimeOffset.Parse(untilInstant, CultureInfo.InvariantCulture).UtcDateTime;
        await CreateMealAsync(client, productId, from.AddMinutes(15), 100);
        await CreateMealAsync(client, productId, second.AddHours(12), 200);
        await CreateMealAsync(client, productId, from.AddTicks(-1), 500);
        await CreateMealAsync(client, productId, until, 1000);
        const string range = "/api/v1/cycles/current/nutrition-summary?dateFrom=2026-08-29&dateTo=2026-08-30";

        using var summary = JsonDocument.Parse(await client.GetStringAsync(range + "&timeZoneId=" + Uri.EscapeDataString(zoneId)));

        JsonElement actual = summary.RootElement;
        Assert.Multiple(
            () => Assert.Equal(2, actual.GetProperty("daysWithMeals").GetInt32()),
            () => Assert.Equal(1, actual.GetProperty("comparableCycles").GetInt32()),
            () => Assert.Equal(200, actual.GetProperty("averageCaloriesOnBleedingDays").GetDouble()),
            () => Assert.Equal(400, actual.GetProperty("averageCaloriesOnNonBleedingCycleDays").GetDouble()),
            () => Assert.Equal(2, actual.GetProperty("averageFiberOnBleedingDays").GetDouble()),
            () => Assert.Equal(4, actual.GetProperty("averageFiberOnNonBleedingCycleDays").GetDouble()));
        Assert.Equal(await client.GetStringAsync(range), await client.GetStringAsync(range + "&timeZoneId=UTC"));

        HttpResponseMessage revoked = await client.PutAsJsonAsync($"/api/v1/cycles/{profileId}/consents/{(int)CycleConsentPurpose.NutritionInsights}", new { granted = false });
        revoked.EnsureSuccessStatusCode();
        using var gated = JsonDocument.Parse(await client.GetStringAsync(range + "&timeZoneId=" + Uri.EscapeDataString(zoneId)));
        Assert.True(gated.RootElement.GetProperty("consentRequired").GetBoolean());
        Assert.Equal(0, gated.RootElement.GetProperty("daysWithMeals").GetInt32());
    }

    [RequiresDockerFact]
    public async Task NutritionSummary_ValidatesZoneAndPreservesClaimOwner() {
        using HttpClient client = factory.CreateClient();
        Guid ownerId = await AuthenticateNewUserAsync(client);
        await CreateCycleAsync(client);
        const string range = "/api/v1/cycles/current/nutrition-summary?dateFrom=2026-08-29&dateTo=2026-08-30";
        foreach (string zone in new[] { "Not/AZone", new string('a', 101) }) {
            HttpResponseMessage invalid = await client.GetAsync(range + "&timeZoneId=" + Uri.EscapeDataString(zone));
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        }
        Assert.Equal(await client.GetStringAsync(range), await client.GetStringAsync(range + "&timeZoneId=%20"));
        using HttpClient other = factory.CreateClient();
        await AuthenticateNewUserAsync(other);
        HttpResponseMessage foreign = await other.GetAsync(range + $"&timeZoneId=Asia%2FTbilisi&userId={ownerId}");
        Assert.Equal(HttpStatusCode.NoContent, foreign.StatusCode);
        using HttpClient anonymous = factory.CreateClient();
        HttpResponseMessage denied = await anonymous.GetAsync(range + "&timeZoneId=Asia%2FTbilisi");
        Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
    }

    private static async Task<Guid> AuthenticateNewUserAsync(HttpClient client) {
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterHttpRequest($"cycle-calendar-{Guid.NewGuid():N}@example.test", "Password123!", "en"));
        response.EnsureSuccessStatusCode();
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Guid userId = body.RootElement.GetProperty("user").GetProperty("id").GetGuid();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.AuthenticateHeader, "true");
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, userId.ToString());
        return userId;
    }

    private static async Task<Guid> CreateCycleAsync(HttpClient client) {
        var request = new CreateCycleHttpRequest(new DateTime(2026, 8, 29, 0, 0, 0, DateTimeKind.Utc), 0,
            AverageCycleLength: 28, AveragePeriodLength: 5, LutealLength: 14, IsRegular: false,
            IsOnboardingComplete: true, ShowFertilityEstimates: false, DiscreetNotifications: true, Notes: null,
            CycleTrackingConsentGranted: true, NutritionInsightsConsentGranted: true);
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/cycles", request);
        response.EnsureSuccessStatusCode();
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("id").GetGuid();
    }

    private static async Task<Guid> CreateProductAsync(HttpClient client) {
        var request = new CreateProductHttpRequest(Barcode: null, "Cycle calendar oracle", Brand: null, "Other",
            Category: null, Description: null, Comment: null, ImageUrl: null, ImageAssetId: null,
            "G", 100, 100, 200, 10, 4, 31, 2, 0, "Private");
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/products", request);
        response.EnsureSuccessStatusCode();
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("id").GetGuid();
    }

    private static async Task CreateMealAsync(HttpClient client, Guid productId, DateTime date, double amount) {
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/meals", new CreateMealHttpRequest(
            date, "Lunch", "Cycle calendar oracle", ImageUrl: null, ImageAssetId: null,
            [new MealItemHttpRequest(productId, RecipeId: null, amount)]));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
