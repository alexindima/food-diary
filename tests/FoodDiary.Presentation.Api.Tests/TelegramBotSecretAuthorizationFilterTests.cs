using FoodDiary.Presentation.Api.Options;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Presentation.Api.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class TelegramBotSecretAuthorizationFilterTests {
    [Fact]
    public async Task OnAuthorizationAsync_WithoutConfiguredSecret_ReturnsServerErrorContract() {
        TelegramBotSecretAuthorizationFilter filter = CreateFilter(string.Empty);
        AuthorizationFilterContext context = CreateContext();

        await filter.OnAuthorizationAsync(context);

        ObjectResult result = Assert.IsType<ObjectResult>(context.Result);
        ApiErrorHttpResponse payload = Assert.IsType<ApiErrorHttpResponse>(result.Value);
        Assert.Equal(StatusCodes.Status500InternalServerError, result.StatusCode);
        Assert.Equal("Authentication.TelegramBotNotConfigured", payload.Error);
        Assert.Equal("Telegram bot authentication is not configured.", payload.Message);
    }

    [Fact]
    public async Task OnAuthorizationAsync_WithInvalidSecret_ReturnsUnauthorizedContract() {
        TelegramBotSecretAuthorizationFilter filter = CreateFilter("expected-secret");
        AuthorizationFilterContext context = CreateContext();
        context.HttpContext.Request.Headers[TelegramBotSecretAuthorizationFilter.SecretHeaderName] = "wrong-secret";

        await filter.OnAuthorizationAsync(context);

        ObjectResult result = Assert.IsType<ObjectResult>(context.Result);
        ApiErrorHttpResponse payload = Assert.IsType<ApiErrorHttpResponse>(result.Value);
        Assert.Equal(StatusCodes.Status401Unauthorized, result.StatusCode);
        Assert.Equal("Authentication.TelegramBotInvalidSecret", payload.Error);
        Assert.Equal("Telegram bot secret is invalid.", payload.Message);
    }

    [Fact]
    public async Task OnAuthorizationAsync_WithValidSecret_AllowsRequest() {
        TelegramBotSecretAuthorizationFilter filter = CreateFilter("expected-secret");
        AuthorizationFilterContext context = CreateContext();
        context.HttpContext.Request.Headers[TelegramBotSecretAuthorizationFilter.SecretHeaderName] = "expected-secret";

        await filter.OnAuthorizationAsync(context);

        Assert.Null(context.Result);
    }

    private static TelegramBotSecretAuthorizationFilter CreateFilter(string apiSecret) {
        IOptions<TelegramBotAuthOptions> options = Microsoft.Extensions.Options.Options.Create(new TelegramBotAuthOptions {
            ApiSecret = apiSecret,
        });

        return new TelegramBotSecretAuthorizationFilter(options, NullLogger<TelegramBotSecretAuthorizationFilter>.Instance);
    }

    private static AuthorizationFilterContext CreateContext() {
        var httpContext = new DefaultHttpContext {
            TraceIdentifier = "trace-123",
        };
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        return new AuthorizationFilterContext(actionContext, []);
    }
}
