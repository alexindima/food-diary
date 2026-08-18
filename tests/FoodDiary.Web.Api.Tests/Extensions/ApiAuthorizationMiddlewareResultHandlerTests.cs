using System.Text.Json;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Web.Api.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Web.Api.Tests.Extensions;

[ExcludeFromCodeCoverage]
public sealed class ApiAuthorizationMiddlewareResultHandlerTests {
    private static readonly AuthorizationPolicy Policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    private static readonly JsonSerializerOptions WebJsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task HandleAsync_WhenAuthorizationSucceeds_InvokesNext() {
        var handler = new ApiAuthorizationMiddlewareResultHandler();
        DefaultHttpContext context = CreateHttpContext(Substitute.For<IAuthenticationService>());
        bool nextCalled = false;

        await handler.HandleAsync(
            _ => {
                nextCalled = true;
                return Task.CompletedTask;
            },
            context,
            Policy,
            PolicyAuthorizationResult.Success());

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task HandleAsync_WhenAuthenticationIsRequired_ChallengesAndWritesStandardError() {
        IAuthenticationService authenticationService = Substitute.For<IAuthenticationService>();
        authenticationService
            .ChallengeAsync(Arg.Any<HttpContext>(), scheme: null, Arg.Any<AuthenticationProperties?>())
            .Returns(Task.CompletedTask);
        var handler = new ApiAuthorizationMiddlewareResultHandler();
        DefaultHttpContext context = CreateHttpContext(authenticationService);

        await handler.HandleAsync(
            static _ => Task.CompletedTask,
            context,
            Policy,
            PolicyAuthorizationResult.Challenge());

        ApiErrorHttpResponse response = await ReadResponseAsync(context);
        await authenticationService.Received(1)
            .ChallengeAsync(context, scheme: null, Arg.Any<AuthenticationProperties?>());
        Assert.Multiple(
            () => Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode),
            () => Assert.StartsWith("application/json", context.Response.ContentType, StringComparison.Ordinal),
            () => Assert.Equal("Authentication.Unauthorized", response.Error),
            () => Assert.Equal("Authentication is required.", response.Message),
            () => Assert.Equal("trace-id", response.TraceId));
    }

    [Fact]
    public async Task HandleAsync_WhenAccessIsForbidden_ForbidsAndWritesStandardError() {
        IAuthenticationService authenticationService = Substitute.For<IAuthenticationService>();
        authenticationService
            .ForbidAsync(Arg.Any<HttpContext>(), scheme: null, Arg.Any<AuthenticationProperties?>())
            .Returns(Task.CompletedTask);
        var handler = new ApiAuthorizationMiddlewareResultHandler();
        DefaultHttpContext context = CreateHttpContext(authenticationService);

        await handler.HandleAsync(
            static _ => Task.CompletedTask,
            context,
            Policy,
            PolicyAuthorizationResult.Forbid());

        ApiErrorHttpResponse response = await ReadResponseAsync(context);
        await authenticationService.Received(1)
            .ForbidAsync(context, scheme: null, Arg.Any<AuthenticationProperties?>());
        Assert.Multiple(
            () => Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode),
            () => Assert.StartsWith("application/json", context.Response.ContentType, StringComparison.Ordinal),
            () => Assert.Equal("Authentication.Forbidden", response.Error),
            () => Assert.Equal("You do not have permission to access this resource.", response.Message),
            () => Assert.Equal("trace-id", response.TraceId));
    }

    [Fact]
    public async Task HandleAsync_WhenChallengeHasStartedResponse_DoesNotAppendErrorBody() {
        IAuthenticationService authenticationService = Substitute.For<IAuthenticationService>();
        authenticationService
            .ChallengeAsync(Arg.Any<HttpContext>(), scheme: null, Arg.Any<AuthenticationProperties?>())
            .Returns(Task.CompletedTask);
        var handler = new ApiAuthorizationMiddlewareResultHandler();
        var responseFeature = new StartedResponseFeature();
        DefaultHttpContext context = CreateHttpContext(authenticationService, responseFeature);

        await handler.HandleAsync(
            static _ => Task.CompletedTask,
            context,
            Policy,
            PolicyAuthorizationResult.Challenge());

        await authenticationService.Received(1)
            .ChallengeAsync(context, scheme: null, Arg.Any<AuthenticationProperties?>());
        Assert.Equal(0, responseFeature.Body.Length);
    }

    private static DefaultHttpContext CreateHttpContext(
        IAuthenticationService authenticationService,
        IHttpResponseFeature? responseFeature = null) {
        ServiceProvider services = new ServiceCollection()
            .AddSingleton(authenticationService)
            .BuildServiceProvider();
        var context = new DefaultHttpContext();
        if (responseFeature is not null) {
            context.Features.Set(responseFeature);
        }

        context.RequestServices = services;
        context.TraceIdentifier = "trace-id";
        if (!context.Response.HasStarted) {
            context.Response.Body = new MemoryStream();
        }

        return context;
    }

    private static async Task<ApiErrorHttpResponse> ReadResponseAsync(DefaultHttpContext context) {
        context.Response.Body.Position = 0;
        ApiErrorHttpResponse? response = await JsonSerializer.DeserializeAsync<ApiErrorHttpResponse>(
            context.Response.Body,
            WebJsonOptions).ConfigureAwait(false);

        Assert.NotNull(response);
        return response;
    }

    [ExcludeFromCodeCoverage]
    private sealed class StartedResponseFeature : IHttpResponseFeature {
        public int StatusCode { get; set; } = StatusCodes.Status200OK;

        public string? ReasonPhrase { get; set; }

        public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();

        public Stream Body { get; set; } = new MemoryStream();

        public bool HasStarted => true;

        public void OnStarting(Func<object, Task> callback, object state) {
        }

        public void OnCompleted(Func<object, Task> callback, object state) {
        }
    }
}
