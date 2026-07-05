using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using FoodDiary.Presentation.Api.Responses;

namespace FoodDiary.Presentation.Api.Filters;

public sealed class IdempotencyFilter(IIdempotencyStore idempotencyStore) : IAsyncActionFilter {
    private const string IdempotencyKeyHeader = "Idempotency-Key";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);
    private static readonly TimeSpan ProcessingDuration = TimeSpan.FromMinutes(5);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

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
        string requestHash = ComputeRequestHash(context);
        IdempotencyReservation reservation = await idempotencyStore
            .ReserveAsync(cacheKey, requestHash, CacheDuration, ProcessingDuration, context.HttpContext.RequestAborted)
            .ConfigureAwait(false);

        if (TryApplyReservation(context, reservation)) {
            return;
        }

        ActionExecutedContext executedContext = await next().ConfigureAwait(false);
        await CacheExecutedResponseAsync(context, executedContext, cacheKey, requestHash).ConfigureAwait(false);
    }

    private static bool TryApplyReservation(ActionExecutingContext context, IdempotencyReservation reservation) {
        if (reservation.Status == IdempotencyReservationStatus.Conflict) {
            context.Result = CreateIdempotencyConflict(context);
            return true;
        }

        if (reservation.Status == IdempotencyReservationStatus.InProgress) {
            context.Result = CreateIdempotencyInProgress(context);
            return true;
        }

        if (reservation.Status != IdempotencyReservationStatus.Replay) {
            return false;
        }

        context.Result = new ContentResult {
            Content = reservation.Body,
            ContentType = "application/json",
            StatusCode = reservation.StatusCode ?? StatusCodes.Status200OK,
        };
        return true;
    }

    private async Task CacheExecutedResponseAsync(
        ActionExecutingContext context,
        ActionExecutedContext executedContext,
        string cacheKey,
        string requestHash) {
        if (executedContext.Exception is not null || executedContext.Result is not ObjectResult objectResult) {
            return;
        }

        await idempotencyStore.CompleteAsync(
            cacheKey,
            requestHash,
            objectResult.StatusCode ?? StatusCodes.Status200OK,
            JsonSerializer.Serialize(objectResult.Value, JsonOptions),
            CacheDuration,
            context.HttpContext.RequestAborted).ConfigureAwait(false);
    }

    private static ObjectResult CreateIdempotencyConflict(ActionExecutingContext context) =>
        new(new ApiErrorHttpResponse(
            "Idempotency.Conflict",
            "The idempotency key was already used with a different request.",
            context.HttpContext.TraceIdentifier)) {
            StatusCode = StatusCodes.Status409Conflict,
        };

    private static ObjectResult CreateIdempotencyInProgress(ActionExecutingContext context) =>
        new(new ApiErrorHttpResponse(
            "Idempotency.InProgress",
            "The idempotency key is already being processed.",
            context.HttpContext.TraceIdentifier)) {
            StatusCode = StatusCodes.Status409Conflict,
        };

    private static string BuildCacheKey(ActionExecutingContext context, string idempotencyKey) {
        string userId = context.HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
        string path = context.HttpContext.Request.Path.Value ?? "";
        return $"idempotency:{userId}:{path}:{idempotencyKey}";
    }

    private static string ComputeRequestHash(ActionExecutingContext context) {
        var payload = new SortedDictionary<string, object?>(context.ActionArguments, StringComparer.Ordinal);
        string serialized = JsonSerializer.Serialize(new {
            Method = context.HttpContext.Request.Method,
            Path = context.HttpContext.Request.Path.Value ?? string.Empty,
            Query = context.HttpContext.Request.QueryString.Value ?? string.Empty,
            Arguments = payload,
        }, JsonOptions);
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(serialized));
        return Convert.ToHexString(hash);
    }
}
