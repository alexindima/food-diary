using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Requests;
using FoodDiary.Modules.Meals.Domain.Entities;
using FoodDiary.Modules.Meals.Domain.ValueObjects;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class DashboardTimeZoneIntegrationTests(PostgresApiWebApplicationFactory factory) : IClassFixture<PostgresApiWebApplicationFactory> {
    [RequiresDockerTheory]
    [InlineData("UTC", "2026-09-20")]
    [InlineData("Asia/Tbilisi", "2026-09-20")]
    [InlineData("America/Los_Angeles", "2026-03-08")]
    [InlineData("America/Los_Angeles", "2026-11-01")]
    [InlineData("America/St_Johns", "2026-09-20")]
    [InlineData("Asia/Kathmandu", "2024-02-29")]
    [InlineData("Pacific/Kiritimati", "2026-01-01")]
    [InlineData("Pacific/Pago_Pago", "2026-01-01")]
    [InlineData("Europe/Berlin", "2026-03-29")]
    [InlineData("Europe/Berlin", "2026-10-25")]
    [InlineData("Australia/Lord_Howe", "2026-10-04")]
    [InlineData("Australia/Lord_Howe", "2026-04-05")]
    [InlineData("Europe/Berlin", "2026-04-01")]
    public async Task Snapshot_UsesCalendarDatesAndExactLocalDays_WithoutAdjacentDayLeaks(string zoneId, string dateText) {
        using HttpClient client = factory.CreateClient();
        string email = $"dashboard-zone-{Guid.NewGuid():N}@example.com";
        using HttpResponseMessage registration = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterHttpRequest(email, "Password123!", "en"));
        registration.EnsureSuccessStatusCode();
        using var auth = JsonDocument.Parse(await registration.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.RootElement.GetProperty("accessToken").GetString());
        var day = DateOnly.ParseExact(dateText, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var zone = TimeZoneInfo.FindSystemTimeZoneById(zoneId);
        DateTime start = Midnight(day, zone);
        DateTime next = Midnight(day.AddDays(1), zone);
        await using (AsyncServiceScope scope = factory.Services.CreateAsyncScope()) {
            FoodDiaryDbContext db = scope.ServiceProvider.GetRequiredService<FoodDiaryDbContext>();
            User user = await db.Users.SingleAsync(user => user.Email == email);
            for (int offset = -6; offset <= 0; offset++) {
                DateOnly bucketDay = day.AddDays(offset);
                AddMeal(Midnight(bucketDay, zone), 100);
                AddMeal(Midnight(bucketDay.AddDays(1), zone).AddMilliseconds(-1), 100);
            }
            AddMeal(start.AddMilliseconds(-1), 1000);
            AddMeal(next, 2000);
            AddMeal(Midnight(day.AddDays(-6), zone).AddMilliseconds(-1), 4000);
            await db.SaveChangesAsync();

            void AddMeal(DateTime timestamp, double calories) {
                var meal = Meal.Create(user.Id, timestamp);
                meal.ApplyNutrition(new MealNutritionUpdate(calories, 10, 5, 15, 2, 0, IsAutoCalculated: false));
                db.Meals.Add(meal);
            }
        }
        for (int offset = -1; offset <= 1; offset++) {
            var date = day.AddDays(offset).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            await PostAsync("/api/v1/weight-entries", new { Date = date, WeightKg = 75 + offset });
            await PostAsync("/api/v1/waist-entries", new { Date = date, CircumferenceCm = 82 + offset });
            await PostAsync("/api/v1/exercises", new { Date = date, ExerciseType = "Walking", DurationMinutes = 30, CaloriesBurned = 300 + (offset * 100) });
        }
        await PostAsync("/api/v1/hydrations", new { TimestampUtc = start.AddMilliseconds(-1), AmountMl = 1000 });
        await PostAsync("/api/v1/hydrations", new { TimestampUtc = start, AmountMl = 100 });
        await PostAsync("/api/v1/hydrations", new { TimestampUtc = next.AddMilliseconds(-1), AmountMl = 100 });
        await PostAsync("/api/v1/hydrations", new { TimestampUtc = next, AmountMl = 2000 });

        string url = $"/api/v1/dashboard?date={dateText}&timeZoneId={Uri.EscapeDataString(zoneId)}&locale=en&trendDays=30";
        using HttpResponseMessage response = await client.GetAsync(url);
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        JsonElement result = payload.RootElement;
        Assert.Equal(JsonValueKind.Object, result.GetProperty("tdeeInsight").ValueKind);
        Assert.Equal(200, result.GetProperty("statistics").GetProperty("totalCalories").GetDouble());
        Assert.Equal(20, result.GetProperty("statistics").GetProperty("averageProteins").GetDouble());
        Assert.Equal(200, result.GetProperty("hydration").GetProperty("totalMl").GetInt32());
        Assert.Equal(300, result.GetProperty("caloriesBurned").GetDouble());
        Assert.Equal(2, result.GetProperty("meals").GetProperty("total").GetInt32());
        Assert.Equal(75, result.GetProperty("weight").GetProperty("latest").GetProperty("weightKg").GetDouble());
        Assert.Equal(82, result.GetProperty("waist").GetProperty("latest").GetProperty("circumferenceCm").GetDouble());
        JsonElement[] weight = [.. result.GetProperty("weightTrend").EnumerateArray()];
        JsonElement[] waist = [.. result.GetProperty("waistTrend").EnumerateArray()];
        Assert.Equal(30, weight.Length);
        Assert.Equal(75, weight[^1].GetProperty("averageWeightKg").GetDouble());
        Assert.Equal(82, waist[^1].GetProperty("averageCircumferenceCm").GetDouble());
        Assert.Equal(dateText, weight[^1].GetProperty("startDate").GetString()![..10]);
        JsonElement[] weekly = [.. result.GetProperty("weeklyCalories").EnumerateArray()];
        Assert.Equal(7, weekly.Length);
        for (int index = 0; index < weekly.Length; index++) {
            Assert.Equal(Midnight(day.AddDays(index - 6), zone), weekly[index].GetProperty("date").GetDateTime());
            Assert.Equal(index == 5 ? 1200 : 200, weekly[index].GetProperty("calories").GetDouble());
        }
        using HttpResponseMessage invalid = await client.GetAsync($"/api/v1/dashboard?date={dateText}&timeZoneId=invalid-zone");
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        using HttpClient anonymous = factory.CreateClient();
        using HttpResponseMessage unauthorized = await anonymous.GetAsync(url);
        Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);

        async Task PostAsync(string path, object body) {
            using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(body) };
            request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("D"));
            using HttpResponseMessage created = await client.SendAsync(request);
            Assert.True(created.IsSuccessStatusCode, await created.Content.ReadAsStringAsync());
        }
    }

    private static DateTime Midnight(DateOnly day, TimeZoneInfo zone) => TimeZoneInfo.ConvertTimeToUtc(day.ToDateTime(TimeOnly.MinValue), zone);
}
