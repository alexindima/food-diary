using System.Security.Claims;
using System.Text.Json;
using FoodDiary.Presentation.Api.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodDiary.Presentation.Api.Tests;

public sealed class IdempotencyFilterTests {
    [Fact]
    public async Task OnActionExecutionAsync_WithCachedPostResponse_ReturnsCachedContent() {
        var cache = new InMemoryDistributedCache();
        var filter = new IdempotencyFilter(cache, NullLogger<IdempotencyFilter>.Instance);
        var httpContext = CreateHttpContext("POST", "/api/v1/products", "key-1", userId: "user-123");
        var cacheKey = "idempotency:user-123:/api/v1/products:key-1";
        await cache.SetStringAsync(cacheKey, "{\"StatusCode\":201,\"Body\":\"{\\u0022id\\u0022:\\u0022cached\\u0022}\"}");

        var context = CreateActionExecutingContext(httpContext);
        var nextCalled = false;

        await filter.OnActionExecutionAsync(context, () => {
            nextCalled = true;
            throw new InvalidOperationException("Should not execute next delegate when cache hits.");
        });

        Assert.False(nextCalled);
        var result = Assert.IsType<ContentResult>(context.Result);
        Assert.Equal(StatusCodes.Status201Created, result.StatusCode);
        Assert.Equal("application/json", result.ContentType);
        Assert.Equal("{\"id\":\"cached\"}", result.Content);
    }

    [Fact]
    public async Task OnActionExecutionAsync_WithSuccessfulPostAndIdempotencyKey_CachesObjectResult() {
        var cache = new InMemoryDistributedCache();
        var filter = new IdempotencyFilter(cache, NullLogger<IdempotencyFilter>.Instance);
        var httpContext = CreateHttpContext("POST", "/api/v1/consumptions", "key-2", userId: "user-456");
        var context = CreateActionExecutingContext(httpContext);

        await filter.OnActionExecutionAsync(context, () => {
            var actionExecuted = new ActionExecutedContext(
                context,
                [],
                controller: new object()) {
                Result = new ObjectResult(new { id = "created", calories = 420 }) {
                    StatusCode = StatusCodes.Status201Created,
                },
            };

            return Task.FromResult(actionExecuted);
        });

        var cached = await cache.GetStringAsync("idempotency:user-456:/api/v1/consumptions:key-2");

        Assert.NotNull(cached);
        using var cacheDoc = JsonDocument.Parse(cached);
        Assert.Equal(StatusCodes.Status201Created, cacheDoc.RootElement.GetProperty("StatusCode").GetInt32());
        var body = cacheDoc.RootElement.GetProperty("Body").GetString();
        Assert.NotNull(body);
        using var bodyDoc = JsonDocument.Parse(body);
        Assert.Equal("created", bodyDoc.RootElement.GetProperty("id").GetString());
        Assert.Null(context.Result);
    }

    [Fact]
    public async Task OnActionExecutionAsync_WithoutIdempotencyKey_DoesNotCache() {
        var cache = new InMemoryDistributedCache();
        var filter = new IdempotencyFilter(cache, NullLogger<IdempotencyFilter>.Instance);
        var httpContext = CreateHttpContext("POST", "/api/v1/products", idempotencyKey: null, userId: "user-789");
        var context = CreateActionExecutingContext(httpContext);

        await filter.OnActionExecutionAsync(context, () => {
            var actionExecuted = new ActionExecutedContext(
                context,
                [],
                controller: new object()) {
                Result = new ObjectResult(new { id = "created" }) {
                    StatusCode = StatusCodes.Status201Created,
                },
            };

            return Task.FromResult(actionExecuted);
        });

        var cached = await cache.GetStringAsync("idempotency:user-789:/api/v1/products:");
        Assert.Null(cached);
    }

    private static DefaultHttpContext CreateHttpContext(string method, string path, string? idempotencyKey, string? userId) {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = method;
        httpContext.Request.Path = path;

        if (!string.IsNullOrWhiteSpace(idempotencyKey)) {
            httpContext.Request.Headers["Idempotency-Key"] = idempotencyKey;
        }

        if (!string.IsNullOrWhiteSpace(userId)) {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, userId)],
                authenticationType: "test"));
        }

        return httpContext;
    }

    private static ActionExecutingContext CreateActionExecutingContext(HttpContext httpContext) {
        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor());

        return new ActionExecutingContext(
            actionContext,
            [],
            new Dictionary<string, object?>(),
            controller: new object());
    }

    private sealed class InMemoryDistributedCache : IDistributedCache {
        private readonly Dictionary<string, byte[]> _entries = new(StringComparer.Ordinal);

        public byte[]? Get(string key) {
            return _entries.TryGetValue(key, out var value) ? value : null;
        }

        public Task<byte[]?> GetAsync(string key, CancellationToken token = default) {
            return Task.FromResult(Get(key));
        }

        public void Refresh(string key) {
        }

        public Task RefreshAsync(string key, CancellationToken token = default) {
            return Task.CompletedTask;
        }

        public void Remove(string key) {
            _entries.Remove(key);
        }

        public Task RemoveAsync(string key, CancellationToken token = default) {
            Remove(key);
            return Task.CompletedTask;
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options) {
            _entries[key] = value;
        }

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default) {
            Set(key, value, options);
            return Task.CompletedTask;
        }
    }
}
