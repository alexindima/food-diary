using System.Diagnostics;
using FoodDiary.Presentation.Api.Extensions;
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

[Collection(PresentationTelemetryCollection.Name)]
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

    [Fact]
    public async Task OnAuthorizationAsync_WithActivityListener_RecordsSuccessAndFailureActivities() {
        using var listener = new TelegramAuthorizationActivityListener();
        TelegramBotSecretAuthorizationFilter filter = CreateFilter("expected-secret");
        AuthorizationFilterContext successContext = CreateContext();
        successContext.HttpContext.Request.Headers[TelegramBotSecretAuthorizationFilter.SecretHeaderName] = "expected-secret";
        AuthorizationFilterContext failureContext = CreateContext();
        failureContext.HttpContext.Request.Headers[TelegramBotSecretAuthorizationFilter.SecretHeaderName] = "wrong-secret";

        await filter.OnAuthorizationAsync(successContext);
        await filter.OnAuthorizationAsync(failureContext);

        Assert.Collection(
            listener.Completed,
            activity => Assert.Equal(ActivityStatusCode.Ok, activity.Status),
            activity => Assert.Multiple(
                () => Assert.Equal(ActivityStatusCode.Error, activity.Status),
                () => Assert.Equal("Authentication.TelegramBotInvalidSecret", activity.GetTagItem("error.type"))));
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

    [ExcludeFromCodeCoverage]
    private sealed class TelegramAuthorizationActivityListener : IDisposable {
        private readonly ActivityListener _listener;

        public TelegramAuthorizationActivityListener() {
            _listener = new ActivityListener {
                ShouldListenTo = source => string.Equals(source.Name, PresentationApiTelemetry.TelemetryName, StringComparison.Ordinal),
                Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                ActivityStopped = activity => {
                    if (string.Equals(activity.OperationName, "auth.telegram.bot-secret", StringComparison.Ordinal)) {
                        Completed.Add(activity);
                    }
                },
            };
            ActivitySource.AddActivityListener(_listener);
        }

        public List<Activity> Completed { get; } = [];

        public void Dispose() => _listener.Dispose();
    }
}
