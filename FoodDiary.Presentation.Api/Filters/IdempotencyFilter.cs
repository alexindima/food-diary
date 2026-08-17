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
    private const int MaximumIdempotencyKeyLength = 128;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);
    private static readonly TimeSpan ProcessingDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan CompletionTimeout = TimeSpan.FromSeconds(5);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next) {
        EnableIdempotencyAttribute? attribute = context.Filters.OfType<EnableIdempotencyAttribute>().FirstOrDefault();
        if (!HttpMethods.IsPost(context.HttpContext.Request.Method) || attribute is null) {
            await next();
            return;
        }

        Microsoft.Extensions.Primitives.StringValues headerValues = context.HttpContext.Request.Headers[IdempotencyKeyHeader];
        if (headerValues.Count > 1) {
            context.Result = CreateInvalidIdempotencyKey(context);
            return;
        }

        string? idempotencyKey = headerValues.Count == 1 ? headerValues[0] : null;

        if (string.IsNullOrWhiteSpace(idempotencyKey)) {
            if (attribute.RequireKey) {
                context.Result = CreateIdempotencyRequired(context);
                return;
            }

            await next();
            return;
        }

        if (!IsValidIdempotencyKey(idempotencyKey)) {
            context.Result = CreateInvalidIdempotencyKey(context);
            return;
        }

        string cacheKey = ComputeCacheKey(context, idempotencyKey);
        IdempotencyRequestContext.SetRequestId(context.HttpContext, cacheKey);
        string requestHash = ComputeRequestHash(context);
        IdempotencyReservation reservation = await idempotencyStore
            .ReserveAsync(cacheKey, requestHash, CacheDuration, ProcessingDuration, context.HttpContext.RequestAborted)
            .ConfigureAwait(false);

        if (TryApplyReservation(context, reservation)) {
            return;
        }

        ActionExecutedContext executedContext = await next().ConfigureAwait(false);
        await CacheExecutedResponseAsync(
            context,
            executedContext,
            cacheKey,
            requestHash,
            reservation.OwnerToken!).ConfigureAwait(false);
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

        int statusCode = reservation.StatusCode ?? StatusCodes.Status200OK;
        context.Result = reservation.Body is null
            ? new StatusCodeResult(statusCode)
            : new ContentResult {
                Content = reservation.Body,
                ContentType = "application/json",
                StatusCode = statusCode,
            };
        return true;
    }

    private async Task CacheExecutedResponseAsync(
        ActionExecutingContext context,
        ActionExecutedContext executedContext,
        string cacheKey,
        string requestHash,
        string ownerToken) {
        if (executedContext.Exception is not null || !TrySerializeResult(executedContext.Result, out int statusCode, out string? body)) {
            return;
        }

        using var completionTimeout = new CancellationTokenSource(CompletionTimeout);
        await idempotencyStore.CompleteAsync(
            cacheKey,
            requestHash,
            ownerToken,
            statusCode,
            body,
            CacheDuration,
            completionTimeout.Token).ConfigureAwait(false);
    }

    private static bool TrySerializeResult(IActionResult? result, out int statusCode, out string? body) {
        switch (result) {
            case ObjectResult objectResult:
                statusCode = objectResult.StatusCode ?? StatusCodes.Status200OK;
                body = JsonSerializer.Serialize(objectResult.Value, JsonOptions);
                return true;
            case StatusCodeResult statusCodeResult:
                statusCode = statusCodeResult.StatusCode;
                body = null;
                return true;
            default:
                statusCode = default;
                body = null;
                return false;
        }
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

    private static ObjectResult CreateIdempotencyRequired(ActionExecutingContext context) =>
        new(new ApiErrorHttpResponse(
            "Idempotency.Required",
            "The Idempotency-Key header is required for this operation.",
            context.HttpContext.TraceIdentifier)) {
            StatusCode = StatusCodes.Status400BadRequest,
        };

    private static ObjectResult CreateInvalidIdempotencyKey(ActionExecutingContext context) =>
        new(new ApiErrorHttpResponse(
            "Idempotency.InvalidKey",
            "The Idempotency-Key header must be 1 to 128 characters using letters, digits, period, underscore, colon, or hyphen.",
            context.HttpContext.TraceIdentifier)) {
            StatusCode = StatusCodes.Status400BadRequest,
        };

    private static bool IsValidIdempotencyKey(string value) {
        if (value.Length > MaximumIdempotencyKeyLength) {
            return false;
        }

        foreach (char character in value) {
            if (!char.IsAsciiLetterOrDigit(character) && character is not ('.' or '_' or ':' or '-')) {
                return false;
            }
        }

        return true;
    }

    private static string ComputeCacheKey(ActionExecutingContext context, string idempotencyKey) {
        string userId = context.HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
        string path = context.HttpContext.Request.Path.Value ?? "";
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{userId}\n{path}\n{idempotencyKey}"));
        return Convert.ToHexString(hash);
    }

    private static string ComputeRequestHash(ActionExecutingContext context) {
        var payload = new SortedDictionary<string, object?>(context.ActionArguments, StringComparer.Ordinal);
        string serialized = JsonSerializer.Serialize(new {
            context.HttpContext.Request.Method,
            Path = context.HttpContext.Request.Path.Value ?? string.Empty,
            Query = context.HttpContext.Request.QueryString.Value ?? string.Empty,
            Arguments = payload,
        }, JsonOptions);
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(serialized));
        return Convert.ToHexString(hash);
    }
}
