using System.Security.Claims;
using FoodDiary.Presentation.Api.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class IdempotencyFilterTests {
    [Fact]
    public async Task OnActionExecutionAsync_WithCompletedPostResponse_ReturnsCachedContent() {
        var store = new InMemoryIdempotencyStore(TimeProvider.System);
        var filter = new IdempotencyFilter(store);
        DefaultHttpContext httpContext = CreateHttpContext("POST", "/api/v1/products", "key-1", userId: "user-123");
        ActionExecutingContext firstContext = CreateActionExecutingContext(httpContext, new EnableIdempotencyAttribute());

        await filter.OnActionExecutionAsync(firstContext, () => Task.FromResult(new ActionExecutedContext(firstContext, [], new object()) {
            Result = new ObjectResult(new { id = "created" }) {
                StatusCode = StatusCodes.Status201Created,
            },
        }));

        DefaultHttpContext replayHttpContext = CreateHttpContext("POST", "/api/v1/products", "key-1", userId: "user-123");
        ActionExecutingContext replayContext = CreateActionExecutingContext(replayHttpContext, new EnableIdempotencyAttribute());
        bool nextCalled = false;

        await filter.OnActionExecutionAsync(replayContext, () => {
            nextCalled = true;
            throw new InvalidOperationException("Should not execute next delegate when store replays.");
        });

        Assert.False(nextCalled);
        ContentResult result = Assert.IsType<ContentResult>(replayContext.Result);
        Assert.Equal(StatusCodes.Status201Created, result.StatusCode);
        Assert.Equal("application/json", result.ContentType);
        Assert.Contains("created", result.Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OnActionExecutionAsync_WithCreatedLocation_ReplaysLocationHeader() {
        var store = new InMemoryIdempotencyStore(TimeProvider.System);
        var filter = new IdempotencyFilter(store);
        DefaultHttpContext firstHttpContext = CreateHttpContext("POST", "/api/v1/products", "key-location", "user-location");
        ActionExecutingContext firstContext = CreateActionExecutingContext(firstHttpContext, new EnableIdempotencyAttribute());

        await filter.OnActionExecutionAsync(firstContext, () => Task.FromResult(new ActionExecutedContext(firstContext, [], new object()) {
            Result = new CreatedResult("/api/v1/products/42", new { id = 42 }),
        }));

        DefaultHttpContext replayHttpContext = CreateHttpContext("POST", "/api/v1/products", "key-location", "user-location");
        ActionExecutingContext replayContext = CreateActionExecutingContext(replayHttpContext, new EnableIdempotencyAttribute());
        await filter.OnActionExecutionAsync(replayContext, () => throw new InvalidOperationException("Replay must skip the action."));

        Assert.Multiple(
            () => Assert.Equal("/api/v1/products/42", replayHttpContext.Response.Headers.Location),
            () => Assert.Equal(StatusCodes.Status201Created, Assert.IsType<ContentResult>(replayContext.Result).StatusCode));
    }

    [Fact]
    public async Task OnActionExecutionAsync_WithLocationBearingObjectResults_CachesResolvedLocation() {
        IUrlHelper urlHelper = Substitute.For<IUrlHelper>();
        urlHelper.Action(Arg.Any<UrlActionContext>()).Returns("https://example.test/api/v1/items/action");
        urlHelper.RouteUrl(Arg.Any<UrlRouteContext>()).Returns("https://example.test/api/v1/items/route");

        await AssertCachedLocationAsync(
            new CreatedAtActionResult("Get", "Items", new { id = 1 }, new { id = 1 }) { UrlHelper = urlHelper },
            "https://example.test/api/v1/items/action");
        await AssertCachedLocationAsync(
            new CreatedAtRouteResult("items", new { id = 1 }, new { id = 1 }) { UrlHelper = urlHelper },
            "https://example.test/api/v1/items/route");
        await AssertCachedLocationAsync(
            new AcceptedResult("/api/v1/jobs/1", new { id = 1 }),
            "/api/v1/jobs/1");
        await AssertCachedLocationAsync(
            new AcceptedAtActionResult("Get", "Jobs", new { id = 1 }, new { id = 1 }) { UrlHelper = urlHelper },
            "https://example.test/api/v1/items/action");
        await AssertCachedLocationAsync(
            new AcceptedAtRouteResult("jobs", new { id = 1 }, new { id = 1 }) { UrlHelper = urlHelper },
            "https://example.test/api/v1/items/route");
    }

    [Fact]
    public async Task OnActionExecutionAsync_WhenStoreMisses_ExecutesNextAndCompletesReservation() {
        var store = new InMemoryIdempotencyStore(TimeProvider.System);
        var filter = new IdempotencyFilter(store);
        DefaultHttpContext httpContext = CreateHttpContext("POST", "/api/v1/meals", "key-2", userId: "user-456");
        ActionExecutingContext context = CreateActionExecutingContext(httpContext, new EnableIdempotencyAttribute());
        bool nextCalled = false;

        await filter.OnActionExecutionAsync(context, () => {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(context, [], new object()) {
                Result = new ObjectResult(new { id = "created", calories = 420 }) {
                    StatusCode = StatusCodes.Status201Created,
                },
            });
        });

        Assert.True(nextCalled);
        Assert.Null(context.Result);
    }

    [Fact]
    public async Task OnActionExecutionAsync_WithNoContentResponse_ReplaysStatusWithoutBody() {
        var store = new InMemoryIdempotencyStore(TimeProvider.System);
        var filter = new IdempotencyFilter(store);
        DefaultHttpContext firstHttpContext = CreateHttpContext("POST", "/api/v1/actions", "key-no-content", "user-204");
        ActionExecutingContext firstContext = CreateActionExecutingContext(firstHttpContext, new EnableIdempotencyAttribute());
        await filter.OnActionExecutionAsync(firstContext, () => Task.FromResult(new ActionExecutedContext(firstContext, [], new object()) {
            Result = new NoContentResult(),
        }));

        DefaultHttpContext replayHttpContext = CreateHttpContext("POST", "/api/v1/actions", "key-no-content", "user-204");
        ActionExecutingContext replayContext = CreateActionExecutingContext(replayHttpContext, new EnableIdempotencyAttribute());
        await filter.OnActionExecutionAsync(replayContext, () => throw new InvalidOperationException("Replay must skip the action."));

        StatusCodeResult result = Assert.IsType<StatusCodeResult>(replayContext.Result);
        Assert.Equal(StatusCodes.Status204NoContent, result.StatusCode);
    }

    [Fact]
    public async Task OnActionExecutionAsync_WithSameKeyAndDifferentPayload_ReturnsConflict() {
        var store = new InMemoryIdempotencyStore(TimeProvider.System);
        var filter = new IdempotencyFilter(store);
        DefaultHttpContext firstHttpContext = CreateHttpContext("POST", "/api/v1/products", "key-conflict", userId: "user-conflict");
        ActionExecutingContext firstContext = CreateActionExecutingContext(
            firstHttpContext,
            new Dictionary<string, object?>(StringComparer.Ordinal) {
                ["request"] = new { Name = "first" },
            },
            new EnableIdempotencyAttribute());

        await filter.OnActionExecutionAsync(firstContext, () => Task.FromResult(new ActionExecutedContext(firstContext, [], new object()) {
            Result = new ObjectResult(new { id = "created-first" }) {
                StatusCode = StatusCodes.Status201Created,
            },
        }));

        DefaultHttpContext secondHttpContext = CreateHttpContext("POST", "/api/v1/products", "key-conflict", userId: "user-conflict");
        ActionExecutingContext secondContext = CreateActionExecutingContext(
            secondHttpContext,
            new Dictionary<string, object?>(StringComparer.Ordinal) {
                ["request"] = new { Name = "second" },
            },
            new EnableIdempotencyAttribute());
        bool nextCalled = false;

        await filter.OnActionExecutionAsync(secondContext, () => {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(secondContext, [], new object()));
        });

        Assert.False(nextCalled);
        ObjectResult conflict = Assert.IsType<ObjectResult>(secondContext.Result);
        Assert.Equal(StatusCodes.Status409Conflict, conflict.StatusCode);
    }

    [Fact]
    public async Task OnActionExecutionAsync_WhenReservationIsInProgress_ReturnsConflict() {
        var filter = new IdempotencyFilter(new FixedIdempotencyStore(
            new IdempotencyReservation(IdempotencyReservationStatus.InProgress)));
        DefaultHttpContext httpContext = CreateHttpContext("POST", "/api/v1/products", "key-busy", userId: "user-busy");
        ActionExecutingContext context = CreateActionExecutingContext(httpContext, new EnableIdempotencyAttribute());
        bool nextCalled = false;

        await filter.OnActionExecutionAsync(context, () => {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(context, [], new object()));
        });

        Assert.False(nextCalled);
        ObjectResult result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(StatusCodes.Status409Conflict, result.StatusCode);
    }

    [Fact]
    public async Task StoreReserveAsync_WhenInProgressReservationExpires_AllowsNewReservation() {
        var timeProvider = new MutableTimeProvider(new DateTime(2026, 7, 8, 10, 0, 0, DateTimeKind.Utc));
        var store = new InMemoryIdempotencyStore(timeProvider);

        IdempotencyReservation first = await store.ReserveAsync(
            "key-expired-processing",
            "hash",
            responseTtl: TimeSpan.FromMinutes(10),
            processingTtl: TimeSpan.FromMinutes(1));
        timeProvider.Advance(TimeSpan.FromMinutes(2));

        IdempotencyReservation second = await store.ReserveAsync(
            "key-expired-processing",
            "hash",
            responseTtl: TimeSpan.FromMinutes(10),
            processingTtl: TimeSpan.FromMinutes(1));

        Assert.Equal(IdempotencyReservationStatus.Acquired, first.Status);
        Assert.Equal(IdempotencyReservationStatus.Acquired, second.Status);
    }

    [Fact]
    public async Task StoreReserveAsync_WhenSameReservationIsStillProcessing_ReturnsInProgress() {
        var store = new InMemoryIdempotencyStore(TimeProvider.System);

        IdempotencyReservation first = await store.ReserveAsync(
            "key-processing",
            "hash",
            responseTtl: TimeSpan.FromMinutes(10),
            processingTtl: TimeSpan.FromMinutes(1));
        IdempotencyReservation second = await store.ReserveAsync(
            "key-processing",
            "hash",
            responseTtl: TimeSpan.FromMinutes(10),
            processingTtl: TimeSpan.FromMinutes(1));

        Assert.Equal(IdempotencyReservationStatus.Acquired, first.Status);
        Assert.Equal(IdempotencyReservationStatus.InProgress, second.Status);
    }

    [Fact]
    public async Task StoreReserveAsync_WhenCompletedReservationExpires_AllowsNewReservation() {
        var timeProvider = new MutableTimeProvider(new DateTime(2026, 7, 8, 10, 0, 0, DateTimeKind.Utc));
        var store = new InMemoryIdempotencyStore(timeProvider);

        IdempotencyReservation first = await store.ReserveAsync(
            "key-expired-response",
            "hash",
            responseTtl: TimeSpan.FromMinutes(1),
            processingTtl: TimeSpan.FromMinutes(10));
        await store.CompleteAsync(
            "key-expired-response",
            "hash",
            first.OwnerToken!,
            StatusCodes.Status201Created,
            "{\"id\":1}",
            location: null,
            responseTtl: TimeSpan.FromMinutes(1));
        timeProvider.Advance(TimeSpan.FromMinutes(2));

        IdempotencyReservation second = await store.ReserveAsync(
            "key-expired-response",
            "hash",
            responseTtl: TimeSpan.FromMinutes(1),
            processingTtl: TimeSpan.FromMinutes(10));

        Assert.Equal(IdempotencyReservationStatus.Acquired, first.Status);
        Assert.Equal(IdempotencyReservationStatus.Acquired, second.Status);
    }

    [Fact]
    public async Task OnActionExecutionAsync_WithoutIdempotencyKey_DoesNotReserve() {
        var store = new RecordingIdempotencyStore();
        var filter = new IdempotencyFilter(store);
        DefaultHttpContext httpContext = CreateHttpContext("POST", "/api/v1/products", idempotencyKey: null, userId: "user-789");
        ActionExecutingContext context = CreateActionExecutingContext(httpContext, new EnableIdempotencyAttribute());

        await filter.OnActionExecutionAsync(context, () => Task.FromResult(new ActionExecutedContext(context, [], new object()) {
            Result = new ObjectResult(new { id = "created" }) {
                StatusCode = StatusCodes.Status201Created,
            },
        }));

        Assert.Equal(0, store.ReserveCalls);
        Assert.Equal(0, store.CompleteCalls);
    }

    [Fact]
    public async Task OnActionExecutionAsync_WithoutEnableIdempotencyAttribute_SkipsStore() {
        var store = new RecordingIdempotencyStore();
        var filter = new IdempotencyFilter(store);
        DefaultHttpContext httpContext = CreateHttpContext("POST", "/api/v1/auth/login", "key-3", userId: "user-000");
        ActionExecutingContext context = CreateActionExecutingContext(httpContext);

        await filter.OnActionExecutionAsync(context, () => Task.FromResult(new ActionExecutedContext(context, [], new object()) {
            Result = new ObjectResult(new { ok = true }) {
                StatusCode = StatusCodes.Status200OK,
            },
        }));

        Assert.Equal(0, store.ReserveCalls);
        Assert.Equal(0, store.CompleteCalls);
    }

    [Fact]
    public async Task OnActionExecutionAsync_WhenActionReturnsNonObjectResult_DoesNotCompleteReservation() {
        var store = new RecordingIdempotencyStore();
        var filter = new IdempotencyFilter(store);
        DefaultHttpContext httpContext = CreateHttpContext("POST", "/api/v1/products", "key-non-object", userId: "user-000");
        ActionExecutingContext context = CreateActionExecutingContext(httpContext, new EnableIdempotencyAttribute());

        await filter.OnActionExecutionAsync(context, () => Task.FromResult(new ActionExecutedContext(context, [], new object()) {
            Result = new EmptyResult(),
        }));

        Assert.Equal(1, store.ReserveCalls);
        Assert.Equal(0, store.CompleteCalls);
    }

    [Fact]
    public async Task OnActionExecutionAsync_WhenActionThrows_DoesNotCompleteReservation() {
        var store = new RecordingIdempotencyStore();
        var filter = new IdempotencyFilter(store);
        DefaultHttpContext httpContext = CreateHttpContext("POST", "/api/v1/products", "key-exception", userId: "user-000");
        ActionExecutingContext context = CreateActionExecutingContext(httpContext, new EnableIdempotencyAttribute());

        await filter.OnActionExecutionAsync(context, () => Task.FromResult(new ActionExecutedContext(context, [], new object()) {
            Exception = new InvalidOperationException("failed"),
            Result = new ObjectResult(new { ignored = true }),
        }));

        Assert.Equal(1, store.ReserveCalls);
        Assert.Equal(0, store.CompleteCalls);
    }

    [Fact]
    public async Task OnActionExecutionAsync_WhenClientDisconnectsAfterAction_CompletesWithServerOwnedToken() {
        var store = new RecordingIdempotencyStore();
        var filter = new IdempotencyFilter(store);
        using var requestCancellation = new CancellationTokenSource();
        DefaultHttpContext httpContext = CreateHttpContext("POST", "/api/v1/products", "key-disconnect", userId: "user-disconnect");
        httpContext.RequestAborted = requestCancellation.Token;
        ActionExecutingContext context = CreateActionExecutingContext(httpContext, new EnableIdempotencyAttribute());

        await filter.OnActionExecutionAsync(context, async () => {
            await requestCancellation.CancelAsync();
            return new ActionExecutedContext(context, [], new object()) {
                Result = new ObjectResult(new { id = "committed" }) {
                    StatusCode = StatusCodes.Status201Created,
                },
            };
        });

        Assert.Multiple(
            () => Assert.Equal(1, store.CompleteCalls),
            () => Assert.False(store.CompletionTokenWasCanceled));
    }

    [Fact]
    public async Task OnActionExecutionAsync_WithValidExternalKey_StoresOnlyHashedKey() {
        var store = new RecordingIdempotencyStore();
        var filter = new IdempotencyFilter(store);
        const string externalKey = "client-visible-key-123";
        DefaultHttpContext httpContext = CreateHttpContext("POST", "/api/v1/products", externalKey, userId: "user-hash");
        ActionExecutingContext context = CreateActionExecutingContext(httpContext, new EnableIdempotencyAttribute());

        await filter.OnActionExecutionAsync(context, () => Task.FromResult(new ActionExecutedContext(context, [], new object()) {
            Result = new StatusCodeResult(StatusCodes.Status202Accepted),
        }));

        Assert.Multiple(
            () => Assert.NotNull(store.LastReservedKey),
            () => Assert.DoesNotContain(externalKey, store.LastReservedKey!, StringComparison.Ordinal),
            () => Assert.Equal(64, store.LastReservedKey!.Length));
    }

    [Fact]
    public async Task OnActionExecutionAsync_WhenKeyIsRequiredAndMissing_RejectsBeforeAction() {
        var store = new RecordingIdempotencyStore();
        var filter = new IdempotencyFilter(store);
        DefaultHttpContext httpContext = CreateHttpContext("POST", "/api/v1/ai/food/text", idempotencyKey: null, userId: "user-required");
        ActionExecutingContext context = CreateActionExecutingContext(httpContext, new EnableIdempotencyAttribute(requireKey: true));

        await filter.OnActionExecutionAsync(context, () => throw new InvalidOperationException("Missing key must not execute the action."));

        ObjectResult result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Multiple(
            () => Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode),
            () => Assert.Equal(0, store.ReserveCalls));
    }

    [Fact]
    public async Task OnActionExecutionAsync_WithMultipleExternalKeys_RejectsBeforeReservation() {
        var store = new RecordingIdempotencyStore();
        var filter = new IdempotencyFilter(store);
        DefaultHttpContext httpContext = CreateHttpContext("POST", "/api/v1/products", idempotencyKey: null, userId: "user-multiple-keys");
        httpContext.Request.Headers["Idempotency-Key"] = new Microsoft.Extensions.Primitives.StringValues(["first", "second"]);
        ActionExecutingContext context = CreateActionExecutingContext(httpContext, new EnableIdempotencyAttribute());

        await filter.OnActionExecutionAsync(context, () => throw new InvalidOperationException("Multiple keys must not execute the action."));

        ObjectResult result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Multiple(
            () => Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode),
            () => Assert.Equal(0, store.ReserveCalls));
    }

    [Theory]
    [InlineData("contains space")]
    [InlineData("contains/slash")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    public async Task OnActionExecutionAsync_WithInvalidExternalKey_RejectsBeforeReservation(string externalKey) {
        var store = new RecordingIdempotencyStore();
        var filter = new IdempotencyFilter(store);
        DefaultHttpContext httpContext = CreateHttpContext("POST", "/api/v1/products", externalKey, userId: "user-invalid-key");
        ActionExecutingContext context = CreateActionExecutingContext(httpContext, new EnableIdempotencyAttribute());

        await filter.OnActionExecutionAsync(context, () => throw new InvalidOperationException("Invalid key must not execute the action."));

        ObjectResult result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Multiple(
            () => Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode),
            () => Assert.Equal(0, store.ReserveCalls));
    }

    private static DefaultHttpContext CreateHttpContext(string method, string? path, string? idempotencyKey, string? userId) {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = method;
        if (path is not null) {
            httpContext.Request.Path = path;
        }

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

    private static async Task AssertCachedLocationAsync(ObjectResult result, string expectedLocation) {
        var store = new RecordingIdempotencyStore();
        var filter = new IdempotencyFilter(store);
        DefaultHttpContext httpContext = CreateHttpContext(
            "POST",
            "/api/v1/items",
            $"key-{Guid.NewGuid():N}",
            "user-location-results");
        ActionExecutingContext context = CreateActionExecutingContext(httpContext, new EnableIdempotencyAttribute());

        await filter.OnActionExecutionAsync(context, () => Task.FromResult(new ActionExecutedContext(context, [], new object()) {
            Result = result,
        }));

        Assert.Equal(expectedLocation, store.LastLocation);
    }

    private static ActionExecutingContext CreateActionExecutingContext(HttpContext httpContext, params IFilterMetadata[] filters) =>
        CreateActionExecutingContext(httpContext, new Dictionary<string, object?>(StringComparer.Ordinal), filters);

    private static ActionExecutingContext CreateActionExecutingContext(
        HttpContext httpContext,
        IDictionary<string, object?> actionArguments,
        params IFilterMetadata[] filters) {
        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor());

        return new ActionExecutingContext(
            actionContext,
            filters,
            actionArguments,
            controller: new object());
    }

    [ExcludeFromCodeCoverage]
    private sealed class FixedIdempotencyStore(IdempotencyReservation reservation) : IIdempotencyStore {
        public Task<IdempotencyReservation> ReserveAsync(
            string key,
            string requestHash,
            TimeSpan responseTtl,
            TimeSpan processingTtl,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(reservation);

        public Task CompleteAsync(
            string key,
            string requestHash,
            string ownerToken,
            int statusCode,
            string? body,
            string? location,
            TimeSpan responseTtl,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingIdempotencyStore : IIdempotencyStore {
        public int ReserveCalls { get; private set; }
        public int CompleteCalls { get; private set; }
        public string? LastReservedKey { get; private set; }
        public string? LastLocation { get; private set; }
        public bool CompletionTokenWasCanceled { get; private set; }

        public Task<IdempotencyReservation> ReserveAsync(
            string key,
            string requestHash,
            TimeSpan responseTtl,
            TimeSpan processingTtl,
            CancellationToken cancellationToken = default) {
            ReserveCalls++;
            LastReservedKey = key;
            return Task.FromResult(new IdempotencyReservation(
                IdempotencyReservationStatus.Acquired,
                OwnerToken: "owner-token"));
        }

        public Task CompleteAsync(
            string key,
            string requestHash,
            string ownerToken,
            int statusCode,
            string? body,
            string? location,
            TimeSpan responseTtl,
            CancellationToken cancellationToken = default) {
            CompleteCalls++;
            LastLocation = location;
            CompletionTokenWasCanceled = cancellationToken.IsCancellationRequested;
            return Task.CompletedTask;
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class MutableTimeProvider(DateTime utcNow) : TimeProvider {
        private DateTime _utcNow = utcNow;

        public override DateTimeOffset GetUtcNow() => new(_utcNow);

        public void Advance(TimeSpan interval) => _utcNow = _utcNow.Add(interval);
    }
}
