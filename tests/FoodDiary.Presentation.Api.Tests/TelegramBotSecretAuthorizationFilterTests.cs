using FoodDiary.Presentation.Api.Options;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Presentation.Api.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FoodDiary.Presentation.Api.Tests;

public sealed class TelegramBotSecretAuthorizationFilterTests {
    [Fact]
    public async Task OnAuthorizationAsync_WithoutConfiguredSecret_ReturnsServerErrorContract() {
        var filter = CreateFilter(string.Empty);
        var context = CreateContext();

        await filter.OnAuthorizationAsync(context);

        var result = Assert.IsType<ObjectResult>(context.Result);
        var payload = Assert.IsType<ApiErrorHttpResponse>(result.Value);
        Assert.Equal(StatusCodes.Status500InternalServerError, result.StatusCode);
        Assert.Equal("Authentication.TelegramBotNotConfigured", payload.Error);
    }

    [Fact]
    public async Task OnAuthorizationAsync_WithInvalidSecret_ReturnsUnauthorizedContract() {
        var filter = CreateFilter("expected-secret");
        var context = CreateContext();
        context.HttpContext.Request.Headers[TelegramBotSecretAuthorizationFilter.SecretHeaderName] = "wrong-secret";

        await filter.OnAuthorizationAsync(context);

        var result = Assert.IsType<ObjectResult>(context.Result);
        var payload = Assert.IsType<ApiErrorHttpResponse>(result.Value);
        Assert.Equal(StatusCodes.Status401Unauthorized, result.StatusCode);
        Assert.Equal("Authentication.TelegramBotInvalidSecret", payload.Error);
    }

    [Fact]
    public async Task OnAuthorizationAsync_WithValidSecret_AllowsRequest() {
        var filter = CreateFilter("expected-secret");
        var context = CreateContext();
        context.HttpContext.Request.Headers[TelegramBotSecretAuthorizationFilter.SecretHeaderName] = "expected-secret";

        await filter.OnAuthorizationAsync(context);

        Assert.Null(context.Result);
    }

    private static TelegramBotSecretAuthorizationFilter CreateFilter(string apiSecret) {
        var options = Microsoft.Extensions.Options.Options.Create(new TelegramBotAuthOptions {
            ApiSecret = apiSecret,
        });

        return new TelegramBotSecretAuthorizationFilter(options);
    }

    private static AuthorizationFilterContext CreateContext() {
        var httpContext = new DefaultHttpContext {
            TraceIdentifier = "trace-123",
        };
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        return new AuthorizationFilterContext(actionContext, []);
    }
}
