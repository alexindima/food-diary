using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FoodDiary.Presentation.Api.Filters;
using FoodDiary.Presentation.Api.Controllers;
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
    public async Task OnActionExecutionAsync_CreatedAtAction_DoesNotUseRequestHostForCachedLocation() {
        IUrlHelper urlHelper = Substitute.For<IUrlHelper>();
        urlHelper.Action(Arg.Any<UrlActionContext>()).Returns("/api/v1/items/1");
        var store = new RecordingIdempotencyStore();
        var filter = new IdempotencyFilter(store);
        DefaultHttpContext httpContext = CreateHttpContext("POST", "/api/v1/items", "relative-location", "user-relative");
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("attacker.example");
        ActionExecutingContext context = CreateActionExecutingContext(httpContext, new EnableIdempotencyAttribute());
        var createdAtAction = new CreatedAtActionResult("Get", "Items", new { id = 1 }, new { id = 1 }) {
            DeclaredType = typeof(object),
            UrlHelper = urlHelper,
        };
        createdAtAction.ContentTypes.Add("application/vnd.fooddiary.test+json");
        var executedContext = new ActionExecutedContext(context, [], new object()) {
            Result = createdAtAction,
        };

        await filter.OnActionExecutionAsync(context, () => Task.FromResult(executedContext));

        urlHelper.Received(1).Action(Arg.Is<UrlActionContext>(urlContext =>
            urlContext.Protocol == null && urlContext.Host == null));
        CreatedResult normalizedResult = Assert.IsType<CreatedResult>(executedContext.Result);
        Assert.Multiple(
            () => Assert.Equal("/api/v1/items/1", store.LastLocation),
            () => Assert.Equal("/api/v1/items/1", normalizedResult.Location),
            () => Assert.Equal(typeof(object), normalizedResult.DeclaredType),
            () => Assert.Contains(
                "application/vnd.fooddiary.test+json",
                normalizedResult.ContentTypes,
                StringComparer.Ordinal));
    }

    [Fact]
    public async Task OnActionExecutionAsync_UsesConfiguredMvcJsonOptionsForReplayBody() {
        var store = new RecordingIdempotencyStore();
        var jsonOptions = new Microsoft.AspNetCore.Mvc.JsonOptions();
        jsonOptions.JsonSerializerOptions.PropertyNamingPolicy = null;
        var filter = new IdempotencyFilter(
            store,
            mvcJsonOptions: Microsoft.Extensions.Options.Options.Create(jsonOptions));
        DefaultHttpContext httpContext = CreateHttpContext("POST", "/api/v1/items", "json-options", "user-json-options");
        ActionExecutingContext context = CreateActionExecutingContext(httpContext, new EnableIdempotencyAttribute());

        await filter.OnActionExecutionAsync(context, () => Task.FromResult(new ActionExecutedContext(context, [], new object()) {
            Result = new ObjectResult(new { PascalCase = true }) { StatusCode = StatusCodes.Status200OK },
        }));

        Assert.Equal("{\"PascalCase\":true}", store.LastBody);
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

    [Theory]
    [InlineData(StatusCodes.Status400BadRequest)]
    [InlineData(StatusCodes.Status500InternalServerError)]
    [InlineData(StatusCodes.Status502BadGateway)]
    public async Task OnActionExecutionAsync_WithUnsuccessfulResponse_ReleasesReservationForRetry(int statusCode) {
        var store = new InMemoryIdempotencyStore(TimeProvider.System);
        var filter = new IdempotencyFilter(store);
        int actionCalls = 0;

        async Task ExecuteAsync(int responseStatusCode) {
            DefaultHttpContext httpContext = CreateHttpContext(
                "POST",
                "/api/v1/ai/food/text",
                "retry-after-failure",
                "retry-user");
            ActionExecutingContext context = CreateActionExecutingContext(httpContext, new EnableIdempotencyAttribute());
            await filter.OnActionExecutionAsync(context, () => {
                actionCalls++;
                return Task.FromResult(new ActionExecutedContext(context, [], new object()) {
                    Result = new ObjectResult(new { attempt = actionCalls }) { StatusCode = responseStatusCode },
                });
            });
        }

        await ExecuteAsync(statusCode);
        await ExecuteAsync(StatusCodes.Status201Created);
        await ExecuteAsync(StatusCodes.Status201Created);

        Assert.Equal(2, actionCalls);
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
    public async Task StoreReleaseAsync_DeletesOnlyTheOwnedIncompleteReservation() {
        var store = new InMemoryIdempotencyStore(TimeProvider.System);
        IdempotencyReservation first = await store.ReserveAsync(
            "key-owned-release",
            "hash",
            responseTtl: TimeSpan.FromMinutes(10),
            processingTtl: TimeSpan.FromMinutes(1));

        await store.ReleaseAsync("key-owned-release", "hash", "stale-owner");
        IdempotencyReservation stillInProgress = await store.ReserveAsync(
            "key-owned-release",
            "hash",
            responseTtl: TimeSpan.FromMinutes(10),
            processingTtl: TimeSpan.FromMinutes(1));
        await store.ReleaseAsync("key-owned-release", "hash", first.OwnerToken!);
        IdempotencyReservation reacquired = await store.ReserveAsync(
            "key-owned-release",
            "hash",
            responseTtl: TimeSpan.FromMinutes(10),
            processingTtl: TimeSpan.FromMinutes(1));

        Assert.Multiple(
            () => Assert.Equal(IdempotencyReservationStatus.InProgress, stillInProgress.Status),
            () => Assert.Equal(IdempotencyReservationStatus.Acquired, reacquired.Status));
    }

    [Fact]
    public async Task StoreReleaseAsync_DoesNotDeleteACompletedResponse() {
        var store = new InMemoryIdempotencyStore(TimeProvider.System);
        IdempotencyReservation reservation = await store.ReserveAsync(
            "key-completed-release",
            "hash",
            responseTtl: TimeSpan.FromMinutes(10),
            processingTtl: TimeSpan.FromMinutes(1));
        await store.CompleteAsync(
            "key-completed-release",
            "hash",
            reservation.OwnerToken!,
            StatusCodes.Status201Created,
            "{\"id\":1}",
            location: null,
            responseTtl: TimeSpan.FromMinutes(10));

        await store.ReleaseAsync("key-completed-release", "hash", reservation.OwnerToken!);
        IdempotencyReservation replay = await store.ReserveAsync(
            "key-completed-release",
            "hash",
            responseTtl: TimeSpan.FromMinutes(10),
            processingTtl: TimeSpan.FromMinutes(1));

        Assert.Equal(IdempotencyReservationStatus.Replay, replay.Status);
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
    public async Task OnActionExecutionAsync_WhenActionReturnsNonObjectResult_ReleasesReservation() {
        var store = new RecordingIdempotencyStore();
        var filter = new IdempotencyFilter(store);
        DefaultHttpContext httpContext = CreateHttpContext("POST", "/api/v1/products", "key-non-object", userId: "user-000");
        ActionExecutingContext context = CreateActionExecutingContext(httpContext, new EnableIdempotencyAttribute());

        await filter.OnActionExecutionAsync(context, () => Task.FromResult(new ActionExecutedContext(context, [], new object()) {
            Result = new EmptyResult(),
        }));

        Assert.Multiple(
            () => Assert.Equal(1, store.ReserveCalls),
            () => Assert.Equal(0, store.CompleteCalls),
            () => Assert.Equal(1, store.ReleaseCalls));
    }

    [Fact]
    public async Task OnActionExecutionAsync_WhenActionReturnsException_ReleasesReservation() {
        var store = new RecordingIdempotencyStore();
        var filter = new IdempotencyFilter(store);
        DefaultHttpContext httpContext = CreateHttpContext("POST", "/api/v1/products", "key-exception", userId: "user-000");
        ActionExecutingContext context = CreateActionExecutingContext(httpContext, new EnableIdempotencyAttribute());

        await filter.OnActionExecutionAsync(context, () => Task.FromResult(new ActionExecutedContext(context, [], new object()) {
            Exception = new InvalidOperationException("failed"),
            Result = new ObjectResult(new { ignored = true }),
        }));

        Assert.Multiple(
            () => Assert.Equal(1, store.ReserveCalls),
            () => Assert.Equal(0, store.CompleteCalls),
            () => Assert.Equal(1, store.ReleaseCalls));
    }

    [Fact]
    public async Task OnActionExecutionAsync_WhenActionDelegateThrows_ReleasesReservationAndPreservesException() {
        var store = new RecordingIdempotencyStore();
        var filter = new IdempotencyFilter(store);
        DefaultHttpContext httpContext = CreateHttpContext("POST", "/api/v1/products", "key-thrown-exception", "throw-user");
        ActionExecutingContext context = CreateActionExecutingContext(httpContext, new EnableIdempotencyAttribute());

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            filter.OnActionExecutionAsync(context, () => throw new InvalidOperationException("original action failure")));

        Assert.Multiple(
            () => Assert.Equal("original action failure", exception.Message),
            () => Assert.Equal(1, store.ReleaseCalls),
            () => Assert.Equal(0, store.CompleteCalls));
    }

    [Fact]
    public async Task OnActionExecutionAsync_WhenReleaseFails_PreservesTheActionException() {
        var store = new RecordingIdempotencyStore(throwOnRelease: true);
        var filter = new IdempotencyFilter(store);
        DefaultHttpContext httpContext = CreateHttpContext("POST", "/api/v1/products", "key-release-failure", "throw-user");
        ActionExecutingContext context = CreateActionExecutingContext(httpContext, new EnableIdempotencyAttribute());

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            filter.OnActionExecutionAsync(context, () => throw new InvalidOperationException("original action failure")));

        Assert.Multiple(
            () => Assert.Equal("original action failure", exception.Message),
            () => Assert.Equal(1, store.ReleaseCalls));
    }

    [Fact]
    public async Task OnActionExecutionAsync_WhenCompletionFails_LeavesReservationLockedAndReturnsActionResult() {
        var store = new RecordingIdempotencyStore(throwOnComplete: true);
        var filter = new IdempotencyFilter(store);
        DefaultHttpContext httpContext = CreateHttpContext("POST", "/api/v1/products", "key-completion-failure", "throw-user");
        ActionExecutingContext context = CreateActionExecutingContext(httpContext, new EnableIdempotencyAttribute());

        await filter.OnActionExecutionAsync(context, () => Task.FromResult(new ActionExecutedContext(context, [], new object()) {
            Result = new StatusCodeResult(StatusCodes.Status202Accepted),
        }));

        Assert.Multiple(
            () => Assert.Equal(1, store.CompleteCalls),
            () => Assert.Equal(0, store.ReleaseCalls));
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
            () => Assert.Equal(64, store.LastReservedKey!.Length),
            () => Assert.Equal(1, store.CompleteCalls),
            () => Assert.Equal(0, store.ReleaseCalls));
    }

    [Fact]
    public async Task OnActionExecutionAsync_WithSameKeyForDifferentUsers_DoesNotReplayAcrossUsers() {
        var store = new InMemoryIdempotencyStore(TimeProvider.System);
        var filter = new IdempotencyFilter(store);
        int actionCalls = 0;

        async Task ExecuteAsync(string userId) {
            DefaultHttpContext httpContext = CreateHttpContext("POST", "/api/v1/products", "shared-key", userId);
            ActionExecutingContext context = CreateActionExecutingContext(httpContext, new EnableIdempotencyAttribute());
            await filter.OnActionExecutionAsync(context, () => {
                actionCalls++;
                return Task.FromResult(new ActionExecutedContext(context, [], new object()) {
                    Result = new ObjectResult(new { userId }) { StatusCode = StatusCodes.Status201Created },
                });
            });
        }

        await ExecuteAsync("first-user");
        await ExecuteAsync("second-user");

        Assert.Equal(2, actionCalls);
    }

    [Fact]
    public async Task OnActionExecutionAsync_WithAnonymousPrincipal_PreservesAnonymousReplay() {
        var store = new InMemoryIdempotencyStore(TimeProvider.System);
        var filter = new IdempotencyFilter(store);
        int actionCalls = 0;

        async Task ExecuteAsync() {
            DefaultHttpContext httpContext = CreateHttpContext("POST", "/api/v1/public", "anonymous-key", userId: null);
            ActionExecutingContext context = CreateActionExecutingContext(httpContext, new EnableIdempotencyAttribute());
            await filter.OnActionExecutionAsync(context, () => {
                actionCalls++;
                return Task.FromResult(new ActionExecutedContext(context, [], new object()) {
                    Result = new StatusCodeResult(StatusCodes.Status202Accepted),
                });
            });
        }

        await ExecuteAsync();
        await ExecuteAsync();

        Assert.Equal(1, actionCalls);
    }

    [Fact]
    public async Task OnActionExecutionAsync_WithSubjectClaim_UsesAuthenticatedUserScope() {
        var userId = Guid.NewGuid();
        DefaultHttpContext firstHttpContext = CreateHttpContext("POST", "/api/v1/products", "subject-key", userId: null);
        firstHttpContext.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", userId.ToString())], "test"));
        var store = new InMemoryIdempotencyStore(TimeProvider.System);
        var filter = new IdempotencyFilter(store);
        ActionExecutingContext firstContext = CreateActionExecutingContext(firstHttpContext, new EnableIdempotencyAttribute());
        await filter.OnActionExecutionAsync(firstContext, () => Task.FromResult(new ActionExecutedContext(firstContext, [], new object()) {
            Result = new ObjectResult(new { id = userId }) { StatusCode = StatusCodes.Status201Created },
        }));

        DefaultHttpContext replayHttpContext = CreateHttpContext("POST", "/api/v1/products", "subject-key", userId: null);
        replayHttpContext.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", userId.ToString())], "test"));
        ActionExecutingContext replayContext = CreateActionExecutingContext(replayHttpContext, new EnableIdempotencyAttribute());
        await filter.OnActionExecutionAsync(replayContext, () => throw new InvalidOperationException("Authenticated subject must replay."));

        Assert.IsType<ContentResult>(replayContext.Result);
    }

    [Fact]
    public async Task OnActionExecutionAsync_WithAuthenticatedPrincipalWithoutValidUserId_FailsClosedBeforeReservation() {
        var store = new RecordingIdempotencyStore();
        var filter = new IdempotencyFilter(store);
        DefaultHttpContext httpContext = CreateHttpContext("POST", "/api/v1/products", "invalid-user-key", userId: null);
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "not-a-guid")], "test"));
        ActionExecutingContext context = CreateActionExecutingContext(httpContext, new EnableIdempotencyAttribute());

        await Assert.ThrowsAsync<CurrentUserUnavailableException>(() =>
            filter.OnActionExecutionAsync(context, () => throw new InvalidOperationException("Action must not execute.")));

        Assert.Equal(0, store.ReserveCalls);
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
            string claimValue = CreateStableUserId(userId).ToString();
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, claimValue)],
                authenticationType: "test"));
        }

        return httpContext;
    }

    private static Guid CreateStableUserId(string value) {
        Span<byte> guidBytes = stackalloc byte[16];
        SHA256.HashData(Encoding.UTF8.GetBytes(value)).AsSpan(0, guidBytes.Length).CopyTo(guidBytes);
        return new Guid(guidBytes);
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

        public Task ReleaseAsync(
            string key,
            string requestHash,
            string ownerToken,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingIdempotencyStore(
        bool throwOnComplete = false,
        bool throwOnRelease = false) : IIdempotencyStore {
        public int ReserveCalls { get; private set; }
        public int CompleteCalls { get; private set; }
        public int ReleaseCalls { get; private set; }
        public string? LastReservedKey { get; private set; }
        public string? LastBody { get; private set; }
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
            LastBody = body;
            LastLocation = location;
            CompletionTokenWasCanceled = cancellationToken.IsCancellationRequested;
            if (throwOnComplete) {
                throw new InvalidOperationException("completion failed");
            }

            return Task.CompletedTask;
        }

        public Task ReleaseAsync(
            string key,
            string requestHash,
            string ownerToken,
            CancellationToken cancellationToken = default) {
            ReleaseCalls++;
            if (throwOnRelease) {
                throw new InvalidOperationException("release failed");
            }

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
