using System.Security.Claims;
using System.Text.Json;
using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Presentation.Api.Security;
using FoodDiary.Web.Api.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodDiary.Web.Api.Tests.Extensions;

[ExcludeFromCodeCoverage]
public sealed class ImpersonationAccessGuardMiddlewareTests {
    private static readonly JsonSerializerOptions CaseInsensitiveJsonOptions = new() {
        PropertyNameCaseInsensitive = true,
    };

    [Fact]
    public async Task InvokeAsync_WithProtectedEndpointAndImpersonatedUser_ReturnsForbiddenErrorContract() {
        DefaultHttpContext context = CreateContext(hasProtectedEndpoint: true, isImpersonated: true);
        bool nextCalled = false;
        var middleware = new ImpersonationAccessGuardMiddleware(_ => {
            nextCalled = true;
            return Task.CompletedTask;
        }, NullLogger<ImpersonationAccessGuardMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);

        context.Response.Body.Position = 0;
        ApiErrorHttpResponse? payload = await JsonSerializer.DeserializeAsync<ApiErrorHttpResponse>(
            context.Response.Body,
            CaseInsensitiveJsonOptions);
        Assert.NotNull(payload);
        Assert.Equal("Authentication.ImpersonationActionForbidden", payload.Error);
        Assert.False(string.IsNullOrWhiteSpace(payload.TraceId));
    }

    [Fact]
    public async Task InvokeAsync_WithProtectedEndpoint_DoesNotLogUserIdentifiersOrRawPath() {
        DefaultHttpContext context = CreateContext(hasProtectedEndpoint: true, isImpersonated: true);
        string actorUserId = context.User.FindFirstValue(JwtImpersonationClaimNames.ActorUserId)!;
        string targetUserId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var logger = new RecordingLogger<ImpersonationAccessGuardMiddleware>();
        var middleware = new ImpersonationAccessGuardMiddleware(_ => Task.CompletedTask, logger);

        await middleware.InvokeAsync(context);

        LogEntry entry = Assert.Single(logger.Entries);
        Assert.Multiple(
            () => Assert.Equal(LogLevel.Warning, entry.Level),
            () => Assert.Contains("/api/v1/users/{userId}/password", entry.Message, StringComparison.Ordinal),
            () => Assert.Contains(context.TraceIdentifier, entry.Message, StringComparison.Ordinal),
            () => Assert.DoesNotContain(actorUserId, entry.Message, StringComparison.Ordinal),
            () => Assert.DoesNotContain(targetUserId, entry.Message, StringComparison.Ordinal),
            () => Assert.DoesNotContain(context.Request.Path.Value!, entry.Message, StringComparison.Ordinal),
            () => Assert.DoesNotContain("ActorUserId", entry.Properties.Keys, StringComparer.Ordinal),
            () => Assert.DoesNotContain("TargetUserId", entry.Properties.Keys, StringComparer.Ordinal),
            () => Assert.DoesNotContain("Path", entry.Properties.Keys, StringComparer.Ordinal));
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task InvokeAsync_WhenEndpointIsNotProtectedOrUserIsNotImpersonated_CallsNext(
        bool hasProtectedEndpoint,
        bool isImpersonated) {
        DefaultHttpContext context = CreateContext(hasProtectedEndpoint, isImpersonated);
        bool nextCalled = false;
        var middleware = new ImpersonationAccessGuardMiddleware(_ => {
            nextCalled = true;
            return Task.CompletedTask;
        }, NullLogger<ImpersonationAccessGuardMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
    }

    private static DefaultHttpContext CreateContext(bool hasProtectedEndpoint, bool isImpersonated) {
        var context = new DefaultHttpContext {
            Response = {
                Body = new MemoryStream(),
            },
        };

        context.TraceIdentifier = "trace-id";
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = $"/api/v1/users/{Guid.NewGuid():D}/password";

        var claims = new List<Claim> {
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
        };

        if (isImpersonated) {
            claims.Add(new Claim(JwtImpersonationClaimNames.IsImpersonation, "true"));
            claims.Add(new Claim(JwtImpersonationClaimNames.ActorUserId, Guid.NewGuid().ToString()));
        }

        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));

        if (hasProtectedEndpoint) {
            context.SetEndpoint(new RouteEndpoint(
                _ => Task.CompletedTask,
                RoutePatternFactory.Parse("/api/v1/users/{userId}/password"),
                order: 0,
                new EndpointMetadataCollection(new BlockImpersonatedAccessAttribute()),
                "protected"));
        }

        return context;
    }

    [ExcludeFromCodeCoverage]
    private sealed record LogEntry(
        LogLevel Level,
        string Message,
        IReadOnlyDictionary<string, object?> Properties);

    [ExcludeFromCodeCoverage]
    private sealed class RecordingLogger<T> : ILogger<T> {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) {
            IReadOnlyDictionary<string, object?> properties = state is IEnumerable<KeyValuePair<string, object?>> values
                ? values.ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.Ordinal)
                : new Dictionary<string, object?>(StringComparer.Ordinal);
            Entries.Add(new LogEntry(logLevel, formatter(state, exception), properties));
        }
    }
}
