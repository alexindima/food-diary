using System.Text.Json;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Web.Api.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

namespace FoodDiary.Web.Api.Tests.Extensions;

[ExcludeFromCodeCoverage]
public sealed class ApiExceptionHandlerTests {
    private static readonly JsonSerializerOptions WebJsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task TryHandleAsync_ForCurrentUserUnavailable_ReturnsUnauthorizedApiError() {
        DefaultHttpContext context = CreateHttpContext();
        var handler = new ApiExceptionHandler(NullLogger<ApiExceptionHandler>.Instance);

        bool handled = await handler.TryHandleAsync(context, new CurrentUserUnavailableException(), CancellationToken.None);

        ApiErrorHttpResponse response = await ReadResponseAsync(context);
        Assert.True(handled);
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        Assert.Equal("Authentication.Unauthorized", response.Error);
        Assert.Equal("Authentication is required.", response.Message);
        Assert.Equal("trace-id", response.TraceId);
    }

    [Fact]
    public async Task TryHandleAsync_ForConcurrencyException_ReturnsConflictApiError() {
        DefaultHttpContext context = CreateHttpContext();
        var logger = new RecordingLogger<ApiExceptionHandler>();
        var handler = new ApiExceptionHandler(logger);

        bool handled = await handler.TryHandleAsync(context, new DbUpdateConcurrencyException("Conflict"), CancellationToken.None);

        ApiErrorHttpResponse response = await ReadResponseAsync(context);
        Assert.True(handled);
        Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);
        Assert.Equal("Concurrency.Conflict", response.Error);
        Assert.Equal("The resource was modified by another request. Please retry.", response.Message);
        Assert.Equal("trace-id", response.TraceId);
        AssertPrivacySafeLog(context, logger, LogLevel.Warning);
    }

    [Fact]
    public async Task TryHandleAsync_ForDuplicateHydrationTimestamp_ReturnsConflictApiError() {
        DefaultHttpContext context = CreateHttpContext();
        var logger = new RecordingLogger<ApiExceptionHandler>();
        var handler = new ApiExceptionHandler(logger);
        var postgresException = new PostgresException(
            "duplicate key",
            "ERROR",
            "ERROR",
            PostgresErrorCodes.UniqueViolation,
            constraintName: "IX_HydrationEntries_User_Timestamp");
        var exception = new DbUpdateException("Duplicate hydration timestamp", postgresException);

        bool handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        ApiErrorHttpResponse response = await ReadResponseAsync(context);
        Assert.Multiple(
            () => Assert.True(handled),
            () => Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode),
            () => Assert.Equal("HydrationEntry.AlreadyExists", response.Error));
        AssertPrivacySafeLog(context, logger, LogLevel.Warning);
    }

    [Fact]
    public async Task TryHandleAsync_ForUnhandledException_ReturnsUnexpectedApiError() {
        DefaultHttpContext context = CreateHttpContext();
        var logger = new RecordingLogger<ApiExceptionHandler>();
        var handler = new ApiExceptionHandler(logger);

        bool handled = await handler.TryHandleAsync(context, new InvalidOperationException("Unexpected"), CancellationToken.None);

        ApiErrorHttpResponse response = await ReadResponseAsync(context);
        Assert.True(handled);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Equal("Server.Unexpected", response.Error);
        Assert.Equal("An unexpected error occurred.", response.Message);
        Assert.Equal("trace-id", response.TraceId);
        AssertPrivacySafeLog(context, logger, LogLevel.Error);
    }

    [Fact]
    public async Task TryHandleAsync_ForPayloadTooLarge_ReturnsPayloadTooLargeApiError() {
        DefaultHttpContext context = CreateHttpContext();
        var handler = new ApiExceptionHandler(NullLogger<ApiExceptionHandler>.Instance);

        bool handled = await handler.TryHandleAsync(
            context,
            new BadHttpRequestException("Sensitive parser detail", StatusCodes.Status413PayloadTooLarge),
            CancellationToken.None);

        ApiErrorHttpResponse response = await ReadResponseAsync(context);
        Assert.True(handled);
        Assert.Equal(StatusCodes.Status413PayloadTooLarge, context.Response.StatusCode);
        Assert.Equal("Request.PayloadTooLarge", response.Error);
        Assert.Equal("The request payload is too large.", response.Message);
        Assert.Equal("trace-id", response.TraceId);
    }

    [Fact]
    public async Task TryHandleAsync_ForClientCancellation_ReturnsClientClosedWithoutErrorBody() {
        using var requestCancellation = new CancellationTokenSource();
        await requestCancellation.CancelAsync();
        DefaultHttpContext context = CreateHttpContext();
        context.RequestAborted = requestCancellation.Token;
        var logger = new RecordingLogger<ApiExceptionHandler>();
        var handler = new ApiExceptionHandler(logger);

        bool handled = await handler.TryHandleAsync(
            context,
            new OperationCanceledException(requestCancellation.Token),
            CancellationToken.None);

        Assert.Multiple(
            () => Assert.True(handled),
            () => Assert.Equal(StatusCodes.Status499ClientClosedRequest, context.Response.StatusCode),
            () => Assert.Equal(0, context.Response.Body.Length));
        AssertPrivacySafeLog(context, logger, LogLevel.Debug);
    }

    [Fact]
    public async Task TryHandleAsync_ForClientCancellationAfterResponseStarted_PreservesResponseStatus() {
        using var requestCancellation = new CancellationTokenSource();
        await requestCancellation.CancelAsync();
        var responseFeature = new StartedResponseFeature {
            StatusCode = StatusCodes.Status202Accepted,
        };
        var context = new DefaultHttpContext();
        context.Features.Set<IHttpResponseFeature>(responseFeature);
        context.RequestAborted = requestCancellation.Token;
        var handler = new ApiExceptionHandler(NullLogger<ApiExceptionHandler>.Instance);

        bool handled = await handler.TryHandleAsync(
            context,
            new OperationCanceledException(requestCancellation.Token),
            CancellationToken.None);

        Assert.Multiple(
            () => Assert.True(handled),
            () => Assert.Equal(StatusCodes.Status202Accepted, context.Response.StatusCode),
            () => Assert.Equal(0, responseFeature.Body.Length));
    }

    private static DefaultHttpContext CreateHttpContext() {
        var context = new DefaultHttpContext {
            TraceIdentifier = "trace-id",
        };
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = $"/api/v1/users/{Guid.NewGuid():D}/password";
        context.SetEndpoint(new RouteEndpoint(
            _ => Task.CompletedTask,
            RoutePatternFactory.Parse("/api/v1/users/{userId:guid}/password"),
            order: 0,
            EndpointMetadataCollection.Empty,
            "user-password"));
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static void AssertPrivacySafeLog(
        DefaultHttpContext context,
        RecordingLogger<ApiExceptionHandler> logger,
        LogLevel expectedLevel) {
        LogEntry entry = Assert.Single(logger.Entries);
        Assert.Multiple(
            () => Assert.Equal(expectedLevel, entry.Level),
            () => Assert.Contains("/api/v1/users/{userId:guid}/password", entry.Message, StringComparison.Ordinal),
            () => Assert.DoesNotContain(context.Request.Path.Value!, entry.Message, StringComparison.Ordinal));
    }

    private static async Task<ApiErrorHttpResponse> ReadResponseAsync(DefaultHttpContext context) {
        context.Response.Body.Position = 0;
        ApiErrorHttpResponse? response = await JsonSerializer.DeserializeAsync<ApiErrorHttpResponse>(
            context.Response.Body,
            WebJsonOptions).ConfigureAwait(false);

        Assert.NotNull(response);
        return response!;
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

    [ExcludeFromCodeCoverage]
    private sealed record LogEntry(LogLevel Level, string Message);

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
            Entries.Add(new LogEntry(logLevel, formatter(state, exception)));
        }
    }
}
