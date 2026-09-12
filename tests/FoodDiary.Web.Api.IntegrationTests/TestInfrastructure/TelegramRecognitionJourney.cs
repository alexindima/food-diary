using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FoodDiary.Application.Abstractions.Ai.Common;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

[ExcludeFromCodeCoverage]
internal static class TelegramRecognitionJourney {
    internal static async Task StartAsync(HttpClient client, IServiceProvider services, Guid jobId, Guid assetId) {
        using HttpResponseMessage trial = await client.PostAsync("/api/v1/billing/trial", content: null);
        Assert.Equal(HttpStatusCode.OK, trial.StatusCode);
        using var botRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/telegram/bot/auth") {
            Content = JsonContent.Create(new { TelegramUserId = 987654321 }),
        };
        botRequest.Headers.Add("X-Telegram-Bot-Secret", "telegram-integration-test-secret");
        using HttpResponseMessage botLogin = await client.SendAsync(botRequest);
        Assert.Equal(HttpStatusCode.OK, botLogin.StatusCode);
        using var authentication = JsonDocument.Parse(await botLogin.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authentication.RootElement.GetProperty("accessToken").GetString());
        var request = new { Id = jobId, ImageAssetId = assetId, Description = "Apple" };
        using HttpResponseMessage withoutConsent = await client.PostAsJsonAsync("/api/v1/ai/food/recognitions", request);
        Assert.Equal(HttpStatusCode.Forbidden, withoutConsent.StatusCode);
        using HttpResponseMessage consent = await client.PostAsync("/api/v1/users/ai-consent", content: null);
        Assert.Equal(HttpStatusCode.NoContent, consent.StatusCode);
        using HttpResponseMessage start = await client.PostAsJsonAsync("/api/v1/ai/food/recognitions", request);
        Assert.Equal(HttpStatusCode.Accepted, start.StatusCode);
        using HttpResponseMessage repeat = await client.PostAsJsonAsync("/api/v1/ai/food/recognitions", request);
        Assert.Equal(HttpStatusCode.Accepted, repeat.StatusCode);
        await using (AsyncServiceScope scope = services.CreateAsyncScope()) {
            IFoodRecognitionProcessor processor = scope.ServiceProvider.GetRequiredService<IFoodRecognitionProcessor>();
            Assert.True(await processor.ProcessNextAsync(CancellationToken.None));
            Assert.False(await processor.ProcessNextAsync(CancellationToken.None));
        }
        using HttpResponseMessage completed = await client.GetAsync($"/api/v1/ai/food/recognitions/{jobId}");
        Assert.Equal(HttpStatusCode.OK, completed.StatusCode);
        using var job = JsonDocument.Parse(await completed.Content.ReadAsStringAsync());
        Assert.Equal("Succeeded", job.RootElement.GetProperty("status").GetString());
        Assert.Equal(52, job.RootElement.GetProperty("nutrition").GetProperty("calories").GetDecimal());
        await VerifyUsageAsync(client);
    }

    internal static async Task VerifyUsageAsync(HttpClient client) {
        using HttpResponseMessage response = await client.GetAsync("/api/v1/ai/usage/me");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var usage = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Multiple(
            () => Assert.Equal(20, usage.RootElement.GetProperty("inputUsed").GetInt64()),
            () => Assert.Equal(10, usage.RootElement.GetProperty("outputUsed").GetInt64()));
    }
}
