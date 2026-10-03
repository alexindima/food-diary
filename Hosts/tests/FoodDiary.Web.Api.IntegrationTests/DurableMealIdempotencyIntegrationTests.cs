using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Requests;
using FoodDiary.Modules.Meals.Presentation.Requests;
using FoodDiary.Modules.Products.Presentation.Requests;
using FoodDiary.Presentation.Api.Filters;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class DurableMealIdempotencyIntegrationTests(PostgresApiWebApplicationFactory factory) : IClassFixture<PostgresApiWebApplicationFactory> {
    [RequiresDockerTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MissingCacheCompletion_ReplaysTheCommittedMealAndOriginalResponse(bool repeat) {
        var cache = new LosingCompletionStore();
        await using WebApplicationFactory<Program> isolated = factory.WithWebHostBuilder(builder => builder.ConfigureServices(services => {
            services.RemoveAll<IIdempotencyStore>();
            services.AddSingleton<IIdempotencyStore>(cache);
        }));
        using HttpClient client = isolated.CreateClient();
        string email = $"durable-meal-{Guid.NewGuid():N}@example.com";
        using HttpResponseMessage registered = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterHttpRequest(email, "Password123!", "en"));
        registered.EnsureSuccessStatusCode();
        using var auth = JsonDocument.Parse(await registered.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.RootElement.GetProperty("accessToken").GetString());
        using HttpResponseMessage productResponse = await client.PostAsJsonAsync("/api/v1/products",
            new CreateProductHttpRequest(Barcode: null, "Receipt ingredient", Brand: null, "Unknown", Category: null,
                Description: null, Comment: null, ImageUrl: null, ImageAssetId: null, "G", 100, 100, 120, 10, 5, 20, 3, 0, "Private"));
        productResponse.EnsureSuccessStatusCode();
        using var product = JsonDocument.Parse(await productResponse.Content.ReadAsStringAsync());
        var mealRequest = new CreateMealHttpRequest(DateTime.UtcNow.Date, "Lunch", "Receipt meal", ImageUrl: null, ImageAssetId: null,
            [new MealItemHttpRequest(product.RootElement.GetProperty("id").GetGuid(), RecipeId: null, 180)]);
        string url = "/api/v1/meals";
        object body = mealRequest;
        Guid? sourceId = null;
        if (repeat) {
            using HttpResponseMessage sourceResponse = await client.PostAsJsonAsync(url, mealRequest);
            sourceResponse.EnsureSuccessStatusCode();
            using var source = JsonDocument.Parse(await sourceResponse.Content.ReadAsStringAsync());
            sourceId = source.RootElement.GetProperty("id").GetGuid();
            url = $"/api/v1/meals/{sourceId}/repeat";
            body = new RepeatMealHttpRequest(DateTime.UtcNow.Date.AddDays(1), "Dinner");
        }
        string key = Guid.NewGuid().ToString("D");
        cache.LoseNextCompletion();
        using HttpResponseMessage lost = await SendAsync();
        Assert.Equal(HttpStatusCode.Conflict, lost.StatusCode);
        using var lostBody = JsonDocument.Parse(await lost.Content.ReadAsStringAsync());
        Assert.Equal("Idempotency.LeaseLost", lostBody.RootElement.GetProperty("error").GetString());
        Guid[] committedIds;
        await using (AsyncServiceScope scope = isolated.Services.CreateAsyncScope()) {
            FoodDiaryDbContext context = scope.ServiceProvider.GetRequiredService<FoodDiaryDbContext>();
            UserId userId = await context.Users.Where(user => user.Email == email).Select(user => user.Id).SingleAsync();
            committedIds = await context.Meals.Where(meal => meal.UserId == userId).Select(meal => meal.Id.Value).ToArrayAsync();
        }
        Assert.Equal(repeat ? 2 : 1, committedIds.Length);
        using HttpResponseMessage replay = await SendAsync();
        Assert.Equal(HttpStatusCode.Created, replay.StatusCode);
        using var replayBody = JsonDocument.Parse(await replay.Content.ReadAsStringAsync());
        Guid replayId = replayBody.RootElement.GetProperty("id").GetGuid();
        Assert.Equal(committedIds.Single(id => id != sourceId), replayId);
        Assert.Single(replayBody.RootElement.GetProperty("items").EnumerateArray());
        Assert.EndsWith(replayId.ToString(), replay.Headers.Location!.ToString(), StringComparison.OrdinalIgnoreCase);
        await using (AsyncServiceScope scope = isolated.Services.CreateAsyncScope()) {
            FoodDiaryDbContext context = scope.ServiceProvider.GetRequiredService<FoodDiaryDbContext>();
            UserId userId = await context.Users.Where(user => user.Email == email).Select(user => user.Id).SingleAsync();
            Assert.Equal(committedIds.Length, await context.Meals.CountAsync(meal => meal.UserId == userId));
        }

        async Task<HttpResponseMessage> SendAsync() {
            using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(body) };
            request.Headers.Add("Idempotency-Key", key);
            return await client.SendAsync(request).ConfigureAwait(false);
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class LosingCompletionStore : IIdempotencyStore {
        private int _loseNext;
        public void LoseNextCompletion() => Interlocked.Exchange(ref _loseNext, 1);
        public Task<IdempotencyReservation> ReserveAsync(string key, string requestHash, TimeSpan responseTtl, TimeSpan processingTtl, CancellationToken cancellationToken = default) =>
            Task.FromResult(new IdempotencyReservation(IdempotencyReservationStatus.Acquired, OwnerToken: Guid.NewGuid().ToString("N")));
        public Task<bool> RenewAsync(string key, string requestHash, string ownerToken, TimeSpan processingTtl, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<bool> CompleteAsync(string key, string requestHash, string ownerToken, int statusCode, string? body, string? location, TimeSpan responseTtl, CancellationToken cancellationToken = default) =>
            Task.FromResult(Interlocked.Exchange(ref _loseNext, 0) == 0);
        public Task ReleaseAsync(string key, string requestHash, string ownerToken, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
