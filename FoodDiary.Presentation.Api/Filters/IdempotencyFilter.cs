using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace FoodDiary.Presentation.Api.Filters;

public sealed class IdempotencyFilter(
    IDistributedCache cache,
    ILogger<IdempotencyFilter> logger) : IAsyncActionFilter {
    private const string IdempotencyKeyHeader = "Idempotency-Key";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next) {
        if (!HttpMethods.IsPost(context.HttpContext.Request.Method) || !context.Filters.OfType<EnableIdempotencyAttribute>().Any()) {
            await next();
            return;
        }

        string? idempotencyKey = context.HttpContext.Request.Headers[IdempotencyKeyHeader].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(idempotencyKey)) {
            await next();
            return;
        }

        string cacheKey = BuildCacheKey(context, idempotencyKey);
        string? cached = await cache.GetStringAsync(cacheKey, context.HttpContext.RequestAborted);

        if (cached is not null) {
            logger.LogInformation("Idempotency cache hit for key {IdempotencyKey}", idempotencyKey);
            IdempotencyCacheEntry? entry = JsonSerializer.Deserialize<IdempotencyCacheEntry>(cached);
            if (entry is not null) {
                context.Result = new ContentResult {
                    Content = entry.Body,
                    ContentType = "application/json",
                    StatusCode = entry.StatusCode,
                };
                return;
            }
        }

        ActionExecutedContext executedContext = await next();

        if (executedContext.Exception is null && executedContext.Result is ObjectResult objectResult) {
            var entry = new IdempotencyCacheEntry(
                objectResult.StatusCode ?? StatusCodes.Status200OK,
                JsonSerializer.Serialize(objectResult.Value));

            await cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(entry),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheDuration },
                context.HttpContext.RequestAborted);
        }
    }

    private static string BuildCacheKey(ActionExecutingContext context, string idempotencyKey) {
        string userId = context.HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
        string path = context.HttpContext.Request.Path.Value ?? "";
        return $"idempotency:{userId}:{path}:{idempotencyKey}";
    }

    private sealed record IdempotencyCacheEntry(int StatusCode, string? Body);
}
