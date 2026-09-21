using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Users.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Web.Api.IntegrationTests;

public sealed partial class PresentationBoundaryIntegrationTests {
    [RequiresDockerFact]
    public async Task GoalHistoryPages_BoundSummariesAndTraverseBothMetricsThroughAuthenticatedHttp() {
        using HttpClient client = apiFactory.CreateClient();
        string token = await RegisterAndGetAccessTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var info = JsonDocument.Parse(await client.GetStringAsync("/api/v1/users/info"));
        Guid userId = info.RootElement.GetProperty("id").GetGuid();
        await using (AsyncServiceScope scope = apiFactory.Services.CreateAsyncScope()) {
            FoodDiaryDbContext context = scope.ServiceProvider.GetRequiredService<FoodDiaryDbContext>();
            User user = await context.Users.SingleAsync(candidate => candidate.Id == userId);
            var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            for (int index = 0; index < 12; index++) {
                user.StartWeightGoal(70, 90, start);
                user.StartWaistGoal(75, 95, start);
                user.CancelWeightGoal(start, 80);
                user.CancelWaistGoal(start, 85);
            }
            user.StartWeightGoal(70, 80, start.AddDays(1));
            user.StartWaistGoal(75, 85, start.AddDays(1));
            await context.SaveChangesAsync();
        }
        foreach (string metric in new[] { "weight", "waist" }) {
            string path = $"/api/v1/users/{metric}-goals/page";
            using var summary = JsonDocument.Parse(await client.GetStringAsync($"/api/v1/{metric}-entries/page-summary"));
            Assert.Equal(2, summary.RootElement.GetProperty("goalHistory").GetArrayLength());
            using var first = JsonDocument.Parse(await client.GetStringAsync(path));
            JsonElement items = first.RootElement.GetProperty("items");
            Assert.Equal(10, items.GetArrayLength());
            Assert.All(items.EnumerateArray(), item => Assert.Equal("Cancelled", item.GetProperty("status").GetString()));
            string? cursor = first.RootElement.GetProperty("nextCursor").GetString();
            Assert.NotNull(cursor);
            using var next = JsonDocument.Parse(await client.GetStringAsync(path + "?cursor=" + Uri.EscapeDataString(cursor)));
            JsonElement remaining = next.RootElement.GetProperty("items");
            Assert.Equal(2, remaining.GetArrayLength());
            Assert.Equal(JsonValueKind.Null, next.RootElement.GetProperty("nextCursor").ValueKind);
            Assert.Equal(12, items.EnumerateArray().Concat(remaining.EnumerateArray())
                .Select(item => item.GetProperty("id").GetGuid()).Distinct().Count());
            using HttpResponseMessage invalid = await client.GetAsync(path + "?cursor=invalid");
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
            using HttpClient anonymous = apiFactory.CreateClient();
            using HttpResponseMessage denied = await anonymous.GetAsync(path);
            Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
        }
    }
}
