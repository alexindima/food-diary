using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Extensions;
using FoodDiary.Presentation.Api.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MvcJsonOptions = Microsoft.AspNetCore.Mvc.JsonOptions;

namespace FoodDiary.Presentation.Api.Filters;

public sealed class IdempotencyFilter(
    IIdempotencyStore idempotencyStore,
    ILogger<IdempotencyFilter>? logger = null,
    IOptions<MvcJsonOptions>? mvcJsonOptions = null) : IAsyncActionFilter {
    private const string IdempotencyKeyHeader = "Idempotency-Key";
    private const int MaximumIdempotencyKeyLength = 128;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);
    private static readonly TimeSpan ProcessingDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan CompletionTimeout = TimeSpan.FromSeconds(5);
    private static readonly JsonSerializerOptions RequestHashJsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ILogger<IdempotencyFilter> _logger = logger ?? NullLogger<IdempotencyFilter>.Instance;
    private readonly JsonSerializerOptions _responseJsonOptions = mvcJsonOptions?.Value.JsonSerializerOptions
        ?? new JsonSerializerOptions(JsonSerializerDefaults.Web);

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

        await ExecuteAndFinalizeAsync(
            context,
            next,
            cacheKey,
            requestHash,
            reservation.OwnerToken!).ConfigureAwait(false);
    }

    private async Task ExecuteAndFinalizeAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next,
        string cacheKey,
        string requestHash,
        string ownerToken) {
        ActionExecutedContext executedContext;
        try {
            executedContext = await next().ConfigureAwait(false);
        } catch {
            await TryReleaseReservationAsync(cacheKey, requestHash, ownerToken).ConfigureAwait(false);
            throw;
        }

        if (executedContext.Exception is not null ||
            !TrySerializeResult(
                context,
                executedContext.Result,
                out int statusCode,
                out string? body,
                out string? location) ||
            !IsSuccessfulStatusCode(statusCode)) {
            await TryReleaseReservationAsync(cacheKey, requestHash, ownerToken).ConfigureAwait(false);
            return;
        }

        if (location is not null) {
            executedContext.Result = NormalizeLocationResult(executedContext.Result, location);
        }

        try {
            using var completionTimeout = new CancellationTokenSource(CompletionTimeout);
            await idempotencyStore.CompleteAsync(
                cacheKey,
                requestHash,
                ownerToken,
                statusCode,
                body,
                location,
                CacheDuration,
                completionTimeout.Token).ConfigureAwait(false);
        } catch (Exception exception) {
            _logger.LogError(
                exception,
                "Failed to persist a completed idempotent response; the reservation will remain locked until it expires");
        }
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
        if (!string.IsNullOrWhiteSpace(reservation.Location)) {
            context.HttpContext.Response.Headers.Location = reservation.Location;
        }

        context.Result = reservation.Body is null
            ? new StatusCodeResult(statusCode)
            : new ContentResult {
                Content = reservation.Body,
                ContentType = "application/json",
                StatusCode = statusCode,
            };
        return true;
    }

    private async Task TryReleaseReservationAsync(string cacheKey, string requestHash, string ownerToken) {
        try {
            using var releaseTimeout = new CancellationTokenSource(CompletionTimeout);
            await idempotencyStore.ReleaseAsync(
                cacheKey,
                requestHash,
                ownerToken,
                releaseTimeout.Token).ConfigureAwait(false);
        } catch (Exception exception) {
            _logger.LogWarning(exception, "Failed to release an incomplete idempotency reservation");
        }
    }

    private static bool IsSuccessfulStatusCode(int statusCode) =>
        statusCode is >= StatusCodes.Status200OK and < StatusCodes.Status300MultipleChoices;

    private bool TrySerializeResult(
        ActionExecutingContext context,
        IActionResult? result,
        out int statusCode,
        out string? body,
        out string? location) {
        switch (result) {
            case ObjectResult objectResult:
                statusCode = objectResult.StatusCode ?? StatusCodes.Status200OK;
                body = JsonSerializer.Serialize(objectResult.Value, _responseJsonOptions);
                location = ResolveLocation(context, objectResult);
                return true;
            case StatusCodeResult statusCodeResult:
                statusCode = statusCodeResult.StatusCode;
                body = null;
                location = null;
                return true;
            default:
                statusCode = default;
                body = null;
                location = null;
                return false;
        }
    }

    private static string? ResolveLocation(ActionExecutingContext context, ObjectResult result) =>
        result switch {
            CreatedResult created => created.Location,
            CreatedAtActionResult createdAtAction => ResolveActionLocation(
                context,
                createdAtAction.UrlHelper,
                createdAtAction.ActionName,
                createdAtAction.ControllerName,
                createdAtAction.RouteValues),
            CreatedAtRouteResult createdAtRoute => ResolveRouteLocation(
                context,
                createdAtRoute.UrlHelper,
                createdAtRoute.RouteName,
                createdAtRoute.RouteValues),
            AcceptedResult accepted => accepted.Location,
            AcceptedAtActionResult acceptedAtAction => ResolveActionLocation(
                context,
                acceptedAtAction.UrlHelper,
                acceptedAtAction.ActionName,
                acceptedAtAction.ControllerName,
                acceptedAtAction.RouteValues),
            AcceptedAtRouteResult acceptedAtRoute => ResolveRouteLocation(
                context,
                acceptedAtRoute.UrlHelper,
                acceptedAtRoute.RouteName,
                acceptedAtRoute.RouteValues),
            _ => null,
        };

    private static string? ResolveActionLocation(
        ActionExecutingContext context,
        IUrlHelper? configuredUrlHelper,
        string? actionName,
        string? controllerName,
        object? routeValues) =>
        ResolveUrlHelper(context, configuredUrlHelper).Action(
            actionName,
            controllerName,
            routeValues);

    private static string? ResolveRouteLocation(
        ActionExecutingContext context,
        IUrlHelper? configuredUrlHelper,
        string? routeName,
        object? routeValues) =>
        ResolveUrlHelper(context, configuredUrlHelper).RouteUrl(
            routeName,
            routeValues);

    private static IUrlHelper ResolveUrlHelper(ActionExecutingContext context, IUrlHelper? configuredUrlHelper) =>
        configuredUrlHelper ?? context.HttpContext.RequestServices
            .GetRequiredService<IUrlHelperFactory>()
            .GetUrlHelper(context);

    private static IActionResult? NormalizeLocationResult(IActionResult? result, string location) =>
        result switch {
            CreatedAtActionResult createdAtAction => CopyObjectResultMetadata(
                createdAtAction,
                new CreatedResult(location, createdAtAction.Value)),
            CreatedAtRouteResult createdAtRoute => CopyObjectResultMetadata(
                createdAtRoute,
                new CreatedResult(location, createdAtRoute.Value)),
            AcceptedAtActionResult acceptedAtAction => CopyObjectResultMetadata(
                acceptedAtAction,
                new AcceptedResult(location, acceptedAtAction.Value)),
            AcceptedAtRouteResult acceptedAtRoute => CopyObjectResultMetadata(
                acceptedAtRoute,
                new AcceptedResult(location, acceptedAtRoute.Value)),
            _ => result,
        };

    private static ObjectResult CopyObjectResultMetadata(ObjectResult source, ObjectResult destination) {
        destination.DeclaredType = source.DeclaredType;
        foreach (string contentType in source.ContentTypes) {
            destination.ContentTypes.Add(contentType);
        }

        return destination;
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
        string userId = ResolveIdempotencyScope(context.HttpContext);
        string path = context.HttpContext.Request.Path.Value ?? "";
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{userId}\n{path}\n{idempotencyKey}"));
        return Convert.ToHexString(hash);
    }

    private static string ResolveIdempotencyScope(HttpContext context) {
        if (context.User.Identity?.IsAuthenticated != true) {
            return "anonymous";
        }

        Guid userId = context.User.GetUserGuid() ?? throw new CurrentUserUnavailableException();
        return userId.ToString("D");
    }

    private static string ComputeRequestHash(ActionExecutingContext context) {
        var payload = new SortedDictionary<string, object?>(context.ActionArguments, StringComparer.Ordinal);
        string serialized = JsonSerializer.Serialize(new {
            context.HttpContext.Request.Method,
            Path = context.HttpContext.Request.Path.Value ?? string.Empty,
            Query = context.HttpContext.Request.QueryString.Value ?? string.Empty,
            Arguments = payload,
        }, RequestHashJsonOptions);
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(serialized));
        return Convert.ToHexString(hash);
    }
}
