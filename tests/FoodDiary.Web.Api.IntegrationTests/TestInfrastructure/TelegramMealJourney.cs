using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;

namespace FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

[ExcludeFromCodeCoverage]
internal static class TelegramMealJourney {
    internal static async Task VerifyAsync(HttpClient client, HttpClient otherUserClient, IServiceProvider services) {
        using HttpResponseMessage upload = await client.PostAsJsonAsync("/api/v1/images/upload-url",
            new { FileName = "food.jpg", ContentType = "image/jpeg", FileSizeBytes = 512 });
        Assert.Equal(HttpStatusCode.OK, upload.StatusCode);
        using var image = JsonDocument.Parse(await upload.Content.ReadAsStringAsync());
        Guid assetId = image.RootElement.GetProperty("assetId").GetGuid();
        using HttpResponseMessage confirm = await client.PostAsync($"/api/v1/images/{assetId}/confirm", content: null);
        Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);
        Guid jobId = await VerifyPremiumRequiredAsync(client, assetId);
        DateTime occurredAt = DateTime.UtcNow.Date.AddHours(12);
        await TelegramRecognitionJourney.StartAsync(client, services, jobId, assetId);
        string route = $"/api/v1/meals/recognitions/{jobId}";
        using HttpResponseMessage saved = await client.PostAsJsonAsync(route, new { OccurredAtUtc = occurredAt });
        Assert.True(saved.StatusCode == HttpStatusCode.OK, saved.IsSuccessStatusCode ? null : await saved.Content.ReadAsStringAsync());
        using var receipt = JsonDocument.Parse(await saved.Content.ReadAsStringAsync());
        Guid mealId = receipt.RootElement.GetProperty("mealId").GetGuid();
        using HttpResponseMessage detail = await client.GetAsync($"/api/v1/meals/{mealId}");
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        using var meal = JsonDocument.Parse(await detail.Content.ReadAsStringAsync());
        Assert.Multiple(
            () => Assert.Equal(occurredAt, meal.RootElement.GetProperty("date").GetDateTime()),
            () => Assert.Equal(assetId, meal.RootElement.GetProperty("imageAssetId").GetGuid()),
            () => Assert.Equal(52, meal.RootElement.GetProperty("totalCalories").GetDouble()),
            () => Assert.Equal(1, meal.RootElement.GetProperty("aiSessions").GetArrayLength()));
        using HttpResponseMessage conflict = await client.PostAsJsonAsync(route, new { OccurredAtUtc = occurredAt.AddHours(1) });
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        using HttpResponseMessage replay = await client.PostAsJsonAsync(route, new { OccurredAtUtc = occurredAt });
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        using var repeated = JsonDocument.Parse(await replay.Content.ReadAsStringAsync());
        Assert.Equal(mealId, repeated.RootElement.GetProperty("mealId").GetGuid());
        await VerifyStatisticsAsync(client, expectedMeals: 1, expectedCalories: 52);
        await VerifyOtherUserCannotAccessAsync(otherUserClient, route, mealId, occurredAt);
        using HttpResponseMessage undo = await client.PostAsync($"{route}/undo", content: null);
        Assert.Equal(HttpStatusCode.OK, undo.StatusCode);
        using HttpResponseMessage afterUndo = await client.PostAsJsonAsync(route, new { OccurredAtUtc = occurredAt });
        Assert.Equal(HttpStatusCode.OK, afterUndo.StatusCode);
        using var undone = JsonDocument.Parse(await afterUndo.Content.ReadAsStringAsync());
        Assert.True(undone.RootElement.GetProperty("undone").GetBoolean());
        using HttpResponseMessage deleted = await client.GetAsync($"/api/v1/meals/{mealId}");
        Assert.Equal(HttpStatusCode.NotFound, deleted.StatusCode);
        await VerifyStatisticsAsync(client, expectedMeals: 0, expectedCalories: 0);
        await TelegramRecognitionJourney.VerifyUsageAsync(client);
    }

    private static async Task<Guid> VerifyPremiumRequiredAsync(HttpClient client, Guid assetId) {
        var jobId = Guid.NewGuid();
        using HttpResponseMessage recognition = await client.PostAsJsonAsync("/api/v1/ai/food/recognitions",
            new { Id = jobId, ImageAssetId = assetId, Description = "Apple" });
        Assert.Equal(HttpStatusCode.Forbidden, recognition.StatusCode);
        using HttpResponseMessage absent = await client.GetAsync($"/api/v1/ai/food/recognitions/{jobId}");
        Assert.Equal(HttpStatusCode.NotFound, absent.StatusCode);
        return jobId;
    }

    private static async Task VerifyStatisticsAsync(HttpClient client, int expectedMeals, double expectedCalories) {
        foreach (int days in new[] { 1, 7 }) {
            string url = string.Create(System.Globalization.CultureInfo.InvariantCulture, $"/api/v1/statistics/diary-summary?days={days}");
            using HttpResponseMessage response = await client.GetAsync(url);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var summary = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Multiple(
                () => Assert.Equal("UTC", summary.RootElement.GetProperty("timeZoneId").GetString()),
                () => Assert.Equal(days, summary.RootElement.GetProperty("calendarDays").GetInt32()),
                () => Assert.Equal(expectedMeals, summary.RootElement.GetProperty("mealCount").GetInt32()),
                () => Assert.Equal(expectedCalories, summary.RootElement.GetProperty("totalCalories").GetDouble()),
                () => Assert.Equal(expectedCalories / days, summary.RootElement.GetProperty("averageCaloriesPerCalendarDay").GetDouble(), precision: 6));
        }
    }

    private static async Task VerifyOtherUserCannotAccessAsync(HttpClient client, string route, Guid mealId, DateTime occurredAt) {
        using HttpResponseMessage registration = await client.PostAsJsonAsync("/api/v1/auth/register",
            new { Email = $"telegram-other-{Guid.NewGuid():N}@example.com", Password = "Password123!", Language = "en" });
        Assert.Equal(HttpStatusCode.OK, registration.StatusCode);
        using var auth = JsonDocument.Parse(await registration.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.RootElement.GetProperty("accessToken").GetString());
        using HttpResponseMessage create = await client.PostAsJsonAsync(route, new { OccurredAtUtc = occurredAt });
        Assert.Equal(HttpStatusCode.NotFound, create.StatusCode);
        using HttpResponseMessage undo = await client.PostAsync($"{route}/undo", content: null);
        Assert.Equal(HttpStatusCode.NotFound, undo.StatusCode);
        using HttpResponseMessage detail = await client.GetAsync($"/api/v1/meals/{mealId}");
        Assert.Equal(HttpStatusCode.NotFound, detail.StatusCode);
    }
}
