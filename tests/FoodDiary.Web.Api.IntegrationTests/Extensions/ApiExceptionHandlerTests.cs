using System.Text.Json;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Web.Api.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodDiary.Web.Api.IntegrationTests.Extensions;

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
        var handler = new ApiExceptionHandler(NullLogger<ApiExceptionHandler>.Instance);

        bool handled = await handler.TryHandleAsync(context, new DbUpdateConcurrencyException("Conflict"), CancellationToken.None);

        ApiErrorHttpResponse response = await ReadResponseAsync(context);
        Assert.True(handled);
        Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);
        Assert.Equal("Concurrency.Conflict", response.Error);
        Assert.Equal("The resource was modified by another request. Please retry.", response.Message);
        Assert.Equal("trace-id", response.TraceId);
    }

    [Fact]
    public async Task TryHandleAsync_ForUnhandledException_ReturnsUnexpectedApiError() {
        DefaultHttpContext context = CreateHttpContext();
        var handler = new ApiExceptionHandler(NullLogger<ApiExceptionHandler>.Instance);

        bool handled = await handler.TryHandleAsync(context, new InvalidOperationException("Unexpected"), CancellationToken.None);

        ApiErrorHttpResponse response = await ReadResponseAsync(context);
        Assert.True(handled);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Equal("Server.Unexpected", response.Error);
        Assert.Equal("An unexpected error occurred.", response.Message);
        Assert.Equal("trace-id", response.TraceId);
    }

    private static DefaultHttpContext CreateHttpContext() {
        var context = new DefaultHttpContext {
            TraceIdentifier = "trace-id",
        };
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/api/test";
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<ApiErrorHttpResponse> ReadResponseAsync(DefaultHttpContext context) {
        context.Response.Body.Position = 0;
        ApiErrorHttpResponse? response = await JsonSerializer.DeserializeAsync<ApiErrorHttpResponse>(
            context.Response.Body,
            WebJsonOptions).ConfigureAwait(false);

        Assert.NotNull(response);
        return response!;
    }
}
