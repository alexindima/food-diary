using FoodDiary.Results;
using FoodDiary.MailInbox.Presentation.Extensions;
using FoodDiary.MailInbox.Presentation.Features.Messages;
using FoodDiary.MailInbox.Presentation.Filters;
using FoodDiary.MailInbox.Presentation.Options;
using FoodDiary.MailInbox.Presentation.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodDiary.MailInbox.Presentation.Tests;

[ExcludeFromCodeCoverage]
public sealed class MailInboxPresentationSecurityTests {
    private const string ValidApiKey = "0123456789abcdef0123456789abcdef";

    [Fact]
    public void MailInboxHttpOptions_RejectsUnsafeBounds() {
        Assert.True(MailInboxHttpOptions.HasValidApiKey(CreateOptions()));

        MailInboxHttpOptions[] invalidOptions = [
            CreateOptions(requireApiKey: false),
            CreateOptions(apiKey: null!),
            CreateOptions(apiKey: "too-short"),
            CreateOptions(apiKey: new string('a', MailInboxHttpOptions.MaxApiKeyLength + 1)),
            CreateOptions(maxConcurrentMessageDetailRequests: 0),
            CreateOptions(maxConcurrentMessageDetailRequests: 65),
            CreateOptions(messageDetailQueueTimeout: TimeSpan.Zero),
            CreateOptions(messageDetailQueueTimeout: TimeSpan.FromSeconds(31)),
            CreateOptions(maxConcurrentReadinessRequests: 0),
            CreateOptions(maxConcurrentReadinessRequests: 5),
            CreateOptions(readinessQueueTimeout: TimeSpan.Zero),
            CreateOptions(readinessQueueTimeout: TimeSpan.FromSeconds(6)),
            CreateOptions(readinessExecutionTimeout: TimeSpan.Zero),
            CreateOptions(readinessExecutionTimeout: TimeSpan.FromSeconds(31)),
        ];

        Assert.All(invalidOptions, static options => Assert.False(MailInboxHttpOptions.HasValidApiKey(options)));
    }

    [Fact]
    public void ApiKeyFilter_UsesExactSingleValueComparison() {
        var filter = new MailInboxApiKeyAuthorizationFilter(
            Microsoft.Extensions.Options.Options.Create(CreateOptions()));
        AuthorizationFilterContext matching = CreateAuthorizationContext();
        matching.HttpContext.Request.Headers["X-MailInbox-Api-Key"] = ValidApiKey;
        AuthorizationFilterContext wrong = CreateAuthorizationContext();
        wrong.HttpContext.Request.Headers["X-MailInbox-Api-Key"] = ValidApiKey[..^1] + "0";
        AuthorizationFilterContext duplicate = CreateAuthorizationContext();
        duplicate.HttpContext.Request.Headers.Append("X-MailInbox-Api-Key", ValidApiKey);
        duplicate.HttpContext.Request.Headers.Append("X-MailInbox-Api-Key", ValidApiKey);

        filter.OnAuthorization(matching);
        filter.OnAuthorization(wrong);
        filter.OnAuthorization(duplicate);

        Assert.Multiple(
            () => Assert.Null(matching.Result),
            () => Assert.IsType<UnauthorizedObjectResult>(wrong.Result),
            () => Assert.IsType<UnauthorizedObjectResult>(duplicate.Result));
    }

    [Fact]
    public void MessageRoutes_InheritApiKeyFilterAndDisableResponseCaching() {
        var services = new ServiceCollection();
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
                ["MailInboxHttp:RequireApiKey"] = "true",
                ["MailInboxHttp:ApiKey"] = ValidApiKey,
            })
            .Build();
        services.AddLogging();
        services.AddMailInboxPresentation(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        ControllerActionDescriptor[] actions = [.. provider
            .GetRequiredService<IActionDescriptorCollectionProvider>()
            .ActionDescriptors.Items
            .OfType<ControllerActionDescriptor>()
            .Where(static descriptor => descriptor.ControllerTypeInfo.AsType() == typeof(MailInboxMessagesController))];
        ResponseCacheAttribute cacheAttribute = Assert.Single(
            typeof(MailInboxMessagesController).GetCustomAttributes(typeof(ResponseCacheAttribute), inherit: true)
                .Cast<ResponseCacheAttribute>());

        Assert.Multiple(
            () => Assert.Equal(3, actions.Length),
            () => Assert.All(actions, action => Assert.Contains(
                action.FilterDescriptors,
                descriptor => descriptor.Filter is ServiceFilterAttribute serviceFilter &&
                              serviceFilter.ServiceType == typeof(MailInboxApiKeyAuthorizationFilter))),
            () => Assert.True(cacheAttribute.NoStore),
            () => Assert.Equal(ResponseCacheLocation.None, cacheAttribute.Location));
    }

    [Theory]
    [InlineData(ErrorKind.Internal, "An unexpected error occurred.")]
    [InlineData(ErrorKind.ExternalFailure, "A dependent service failed.")]
    public void ErrorResult_HidesUnsafeMessagesAndDetails(ErrorKind kind, string expectedMessage) {
        IActionResult actionResult = MailInboxResultExtensions.ErrorResult(
            new Error(
                "Sensitive.Provider.Error",
                "Host=db;Password=secret",
                kind,
                new Dictionary<string, string[]>(StringComparer.Ordinal) {
                    ["provider"] = ["secret"],
                }),
            "trace-safe");

        ObjectResult result = Assert.IsType<ObjectResult>(actionResult);
        MailInboxApiErrorHttpResponse response = Assert.IsType<MailInboxApiErrorHttpResponse>(result.Value);
        Assert.Multiple(
            () => Assert.Equal(expectedMessage, response.Message),
            () => Assert.Null(response.Errors),
            () => Assert.Equal("trace-safe", response.TraceId));
    }

    [Fact]
    public void ExceptionFilter_ReturnsSafeInternalResponse() {
        var filter = new MailInboxExceptionFilter(NullLogger<MailInboxExceptionFilter>.Instance);
        ExceptionContext context = CreateExceptionContext(new InvalidOperationException("Password=secret"));

        filter.OnException(context);

        ObjectResult result = Assert.IsType<ObjectResult>(context.Result);
        MailInboxApiErrorHttpResponse response = Assert.IsType<MailInboxApiErrorHttpResponse>(result.Value);
        Assert.Multiple(
            () => Assert.True(context.ExceptionHandled),
            () => Assert.Equal(StatusCodes.Status500InternalServerError, result.StatusCode),
            () => Assert.Equal("MailInbox.Internal", response.Error),
            () => Assert.Equal("An unexpected error occurred.", response.Message),
            () => Assert.DoesNotContain("secret", response.Message, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ExceptionFilter_WhenRequestWasCanceled_DoesNotWriteResponse() {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        ExceptionContext context = CreateExceptionContext(new OperationCanceledException(cancellation.Token));
        context.HttpContext.RequestAborted = cancellation.Token;
        var filter = new MailInboxExceptionFilter(NullLogger<MailInboxExceptionFilter>.Instance);

        filter.OnException(context);

        Assert.Multiple(
            () => Assert.False(context.ExceptionHandled),
            () => Assert.Null(context.Result));
    }

    private static MailInboxHttpOptions CreateOptions(
        bool requireApiKey = true,
        string apiKey = ValidApiKey,
        int maxConcurrentMessageDetailRequests = 2,
        TimeSpan? messageDetailQueueTimeout = null,
        int maxConcurrentReadinessRequests = 1,
        TimeSpan? readinessQueueTimeout = null,
        TimeSpan? readinessExecutionTimeout = null) =>
        new() {
            RequireApiKey = requireApiKey,
            ApiKey = apiKey,
            MaxConcurrentMessageDetailRequests = maxConcurrentMessageDetailRequests,
            MessageDetailQueueTimeout = messageDetailQueueTimeout ?? TimeSpan.FromSeconds(5),
            MaxConcurrentReadinessRequests = maxConcurrentReadinessRequests,
            ReadinessQueueTimeout = readinessQueueTimeout ?? TimeSpan.FromMilliseconds(250),
            ReadinessExecutionTimeout = readinessExecutionTimeout ?? TimeSpan.FromSeconds(5),
        };

    private static AuthorizationFilterContext CreateAuthorizationContext() =>
        new(
            new ActionContext(
                new DefaultHttpContext(),
                new RouteData(),
                new ActionDescriptor()),
            []);

    private static ExceptionContext CreateExceptionContext(Exception exception) {
        var httpContext = new DefaultHttpContext {
            TraceIdentifier = "trace-exception",
        };
        return new ExceptionContext(
            new ActionContext(httpContext, new RouteData(), new ActionDescriptor()),
            []) {
            Exception = exception,
        };
    }
}
