using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

[ExcludeFromCodeCoverage]
internal static class TelegramDeletionJourney {
    internal static async Task VerifyAsync(HttpClient client) {
        const string route = "/api/v1/auth/telegram/bot/operations";
        using HttpResponseMessage unauthorized = await client.GetAsync($"{route}/ready");
        Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);
        client.DefaultRequestHeaders.Add("X-Telegram-Bot-Secret", "telegram-integration-test-secret");
        using HttpResponseMessage register = await client.PostAsJsonAsync(route,
            new { UpdateId = 1234, TelegramUserId = 987654321, Payload = "pending-photo" });
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);
        using var registration = JsonDocument.Parse(await register.Content.ReadAsStringAsync());
        Guid id = registration.RootElement.GetProperty("operationId").GetGuid();
        using HttpResponseMessage acquire = await client.PostAsync($"{route}/{id}/lease", content: null);
        Assert.Equal(HttpStatusCode.OK, acquire.StatusCode);
        using var lease = JsonDocument.Parse(await acquire.Content.ReadAsStringAsync());
        using HttpResponseMessage delete = await client.DeleteAsync("/api/v1/users");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        using HttpResponseMessage profile = await client.GetAsync("/api/v1/users/info");
        Assert.Equal(HttpStatusCode.Unauthorized, profile.StatusCode);
        client.DefaultRequestHeaders.Authorization = null;
        using HttpResponseMessage checkpoint = await client.PostAsJsonAsync($"{route}/{id}/checkpoint",
            new {
                LeaseId = lease.RootElement.GetProperty("leaseId").GetGuid(),
                Checkpoint = "recognizing",
                Completed = false,
                NextAttemptAtUtc = DateTime.UtcNow,
            });
        Assert.Equal(HttpStatusCode.Conflict, checkpoint.StatusCode);
        using HttpResponseMessage reacquire = await client.PostAsync($"{route}/{id}/lease", content: null);
        Assert.Equal(HttpStatusCode.Conflict, reacquire.StatusCode);
        using HttpResponseMessage ready = await client.GetAsync($"{route}/ready");
        Assert.Equal(HttpStatusCode.OK, ready.StatusCode);
        Assert.Empty((await ready.Content.ReadFromJsonAsync<Guid[]>())!);
        using HttpResponseMessage newOperation = await client.PostAsJsonAsync(route,
            new { UpdateId = 1235, TelegramUserId = 987654321, Payload = "new-photo" });
        Assert.Equal(HttpStatusCode.Unauthorized, newOperation.StatusCode);
        using HttpResponseMessage botLogin = await client.PostAsJsonAsync("/api/v1/auth/telegram/bot/auth",
            new { TelegramUserId = 987654321 });
        Assert.Equal(HttpStatusCode.Unauthorized, botLogin.StatusCode);
    }
}
