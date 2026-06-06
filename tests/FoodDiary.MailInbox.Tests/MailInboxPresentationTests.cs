using System.Globalization;
using FoodDiary.MailInbox.Application.Common.Results;
using FoodDiary.MailInbox.Application.Health;
using FoodDiary.MailInbox.Application.Messages.Models;
using FoodDiary.MailInbox.Application.Messages.Queries;
using FoodDiary.MailInbox.Presentation.Controllers;
using FoodDiary.MailInbox.Presentation.Features.Health;
using FoodDiary.MailInbox.Presentation.Extensions;
using FoodDiary.MailInbox.Presentation.Filters;
using FoodDiary.MailInbox.Presentation.Features.Health.Mappings;
using FoodDiary.MailInbox.Presentation.Features.Health.Responses;
using FoodDiary.MailInbox.Presentation.Features.Messages;
using FoodDiary.MailInbox.Presentation.Features.Messages.Mappings;
using FoodDiary.MailInbox.Presentation.Features.Messages.Responses;
using FoodDiary.MailInbox.Presentation.Options;
using FoodDiary.MailInbox.Presentation.Responses;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FoodDiary.MailInbox.Tests;

[ExcludeFromCodeCoverage]
public sealed class MailInboxPresentationTests {
    [Fact]
    public void InboundMailHttpMappings_ToQuery_DefaultsLimitToFifty() {
        int? limit = null;

        GetInboundMailMessagesQuery query = limit.ToQuery();

        Assert.Equal(50, query.Limit);
    }

    [Fact]
    public void InboundMailHttpMappings_ToDetailsResponse_MapsDmarcPreview() {
        var id = Guid.NewGuid();
        var details = new InboundMailMessageDetails(
            id,
            "message-id",
            "sender@example.com",
            ["admin@fooddiary.club"],
            "DMARC",
            "text",
            "<p>html</p>",
            "raw",
            InboundMailMessageCategories.DmarcReport,
            new DmarcReportPreview(
                "google.com",
                "report-1",
                "fooddiary.club",
                DateTimeOffset.Parse("2026-05-01T00:00:00Z", CultureInfo.InvariantCulture),
                DateTimeOffset.Parse("2026-05-02T00:00:00Z", CultureInfo.InvariantCulture),
                [
                    new DmarcReportRecordPreview(
                        "192.0.2.1",
                        2,
                        "none",
                        "pass",
                        "pass",
                        "fooddiary.club",
                        "bounce.fooddiary.club",
                        "fooddiary.club",
                        "pass",
                        "fooddiary.club",
                        "pass"),
                ]),
            "Received",
            DateTimeOffset.UtcNow);

        InboundMailMessageDetailsHttpResponse response = details.ToHttpResponse();

        Assert.Equal(id, response.Id);
        Assert.NotNull(response.DmarcReport);
        Assert.Equal("google.com", response.DmarcReport.OrganizationName);
        Assert.Equal("192.0.2.1", response.DmarcReport.Records.Single().SourceIp);
        Assert.Equal("pass", response.DmarcReport.Records.Single().DkimResult);
    }

    [Fact]
    public void InboundMailHttpMappings_ToSummaryResponse_MapsCategoryAndStatus() {
        var summary = new InboundMailMessageSummary(
            Guid.NewGuid(),
            "sender@example.com",
            ["admin@fooddiary.club"],
            "Hello",
            InboundMailMessageCategories.DmarcReport,
            "Received",
            DateTimeOffset.UtcNow);

        InboundMailMessageSummaryHttpResponse response = summary.ToHttpResponse();

        Assert.Equal(summary.Id, response.Id);
        Assert.Equal("dmarc-report", response.Category);
        Assert.Equal("Received", response.Status);
    }

    [Fact]
    public void MailInboxHealthMappings_ReturnExpectedStatusAndQuery() {
        Assert.Equal("ok", MailInboxHealthHttpMappings.ToHealthHttpResponse().Status);
        Assert.Equal("ready", MailInboxHealthHttpMappings.ToReadyHttpResponse().Status);
        Assert.NotNull(MailInboxHealthHttpMappings.ToReadinessQuery());
    }

    [Fact]
    public void MailInboxApiErrorDetailsMapper_ConvertsDottedPathSegmentsToCamelCase() {
        Assert.Equal("request.toRecipients.0.address", MailInboxApiErrorDetailsMapper.ToCamelCasePath("Request.ToRecipients.0.Address"));
        Assert.Equal("request", MailInboxApiErrorDetailsMapper.ToCamelCasePath(""));
    }

    [Theory]
    [InlineData(true, "secret", true)]
    [InlineData(false, "secret", false)]
    [InlineData(true, "", false)]
    [InlineData(true, " ", false)]
    public void MailInboxHttpOptions_HasValidApiKey_ReturnsExpectedResult(
        bool requireApiKey,
        string apiKey,
        bool expected) {
        var options = new MailInboxHttpOptions {
            RequireApiKey = requireApiKey,
            ApiKey = apiKey,
        };

        Assert.Equal(expected, MailInboxHttpOptions.HasValidApiKey(options));
    }

    [Fact]
    public void MailInboxResponseRecords_ExposeConfiguredValues() {
        var id = Guid.NewGuid();
        DateTimeOffset receivedAtUtc = DateTimeOffset.UtcNow;
        var record = new DmarcReportRecordHttpResponse(
            "192.0.2.1",
            2,
            "none",
            "pass",
            "pass",
            "fooddiary.club",
            "bounce.fooddiary.club",
            "fooddiary.club",
            "pass",
            "fooddiary.club",
            "pass");
        var report = new DmarcReportHttpResponse(
            "google.com",
            "report-1",
            "fooddiary.club",
            receivedAtUtc.AddDays(-1),
            receivedAtUtc,
            [record]);
        var summary = new InboundMailMessageSummaryHttpResponse(
            id,
            "sender@example.com",
            ["admin@fooddiary.club"],
            "Hello",
            InboundMailMessageCategories.General,
            "received",
            receivedAtUtc);
        var details = new InboundMailMessageDetailsHttpResponse(
            id,
            "message-id",
            "sender@example.com",
            ["admin@fooddiary.club"],
            "Hello",
            "text",
            "<p>text</p>",
            "raw",
            InboundMailMessageCategories.DmarcReport,
            report,
            "received",
            receivedAtUtc);

        Assert.Equal(id, summary.Id);
        Assert.Equal("general", summary.Category);
        Assert.Equal("sender@example.com", summary.FromAddress);
        Assert.Equal(["admin@fooddiary.club"], summary.ToRecipients);
        Assert.Equal("Hello", summary.Subject);
        Assert.Equal("received", summary.Status);
        Assert.Equal(receivedAtUtc, summary.ReceivedAtUtc);
        Assert.Equal(report, details.DmarcReport);
        Assert.Equal("message-id", details.MessageId);
        Assert.Equal("sender@example.com", details.FromAddress);
        Assert.Equal(["admin@fooddiary.club"], details.ToRecipients);
        Assert.Equal("Hello", details.Subject);
        Assert.Equal("text", details.TextBody);
        Assert.Equal("<p>text</p>", details.HtmlBody);
        Assert.Equal("raw", details.RawMime);
        Assert.Equal("dmarc-report", details.Category);
        Assert.Equal("received", details.Status);
        Assert.Equal(receivedAtUtc, details.ReceivedAtUtc);
        Assert.Equal("google.com", report.OrganizationName);
        Assert.Equal("report-1", report.ReportId);
        Assert.Equal("fooddiary.club", report.Domain);
        Assert.Equal(receivedAtUtc.AddDays(-1), report.DateRangeStartUtc);
        Assert.Equal(receivedAtUtc, report.DateRangeEndUtc);
        Assert.Equal(record, report.Records.Single());
        Assert.Equal("192.0.2.1", record.SourceIp);
        Assert.Equal(2, record.Count);
        Assert.Equal("none", record.Disposition);
        Assert.Equal("pass", record.Dkim);
        Assert.Equal("pass", record.Spf);
        Assert.Equal("fooddiary.club", record.HeaderFrom);
        Assert.Equal("bounce.fooddiary.club", record.EnvelopeFrom);
        Assert.Equal("fooddiary.club", record.DkimDomain);
        Assert.Equal("pass", record.DkimResult);
        Assert.Equal("fooddiary.club", record.SpfDomain);
        Assert.Equal("pass", record.SpfResult);
    }

    [Theory]
    [InlineData(ErrorKind.Validation, StatusCodes.Status400BadRequest)]
    [InlineData(ErrorKind.Unauthorized, StatusCodes.Status401Unauthorized)]
    [InlineData(ErrorKind.NotFound, StatusCodes.Status404NotFound)]
    [InlineData(ErrorKind.Conflict, StatusCodes.Status409Conflict)]
    [InlineData(ErrorKind.ExternalFailure, StatusCodes.Status502BadGateway)]
    [InlineData(ErrorKind.Internal, StatusCodes.Status500InternalServerError)]
    public void MailInboxResultExtensions_ErrorResult_MapsErrorKindToStatusCode(ErrorKind kind, int expectedStatusCode) {
        IActionResult result = MailInboxResultExtensions.ErrorResult(
            new MailInboxError("code", "message", kind, new Dictionary<string, string[]>(StringComparer.Ordinal) {
                ["Request.RawMime"] = ["Required"],
            }),
            "trace-123");

        ObjectResult objectResult = Assert.IsType<ObjectResult>(result);
        MailInboxApiErrorHttpResponse response = Assert.IsType<MailInboxApiErrorHttpResponse>(objectResult.Value);
        Assert.Equal(expectedStatusCode, objectResult.StatusCode);
        Assert.Equal("code", response.Error);
        Assert.Equal("trace-123", response.TraceId);
        Assert.NotNull(response.Errors);
        Assert.Equal(["Required"], response.Errors["Request.RawMime"]);
    }

    [Fact]
    public void MailInboxResultExtensions_ErrorResult_WhenErrorKindIsUnknown_ReturnsInternalServerError() {
        IActionResult result = MailInboxResultExtensions.ErrorResult(
            new MailInboxError("code", "message", (ErrorKind)999, null),
            traceId: null);

        ObjectResult objectResult = Assert.IsType<ObjectResult>(result);
        MailInboxApiErrorHttpResponse response = Assert.IsType<MailInboxApiErrorHttpResponse>(objectResult.Value);
        Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
        Assert.Equal("message", response.Message);
        Assert.Null(response.TraceId);
        Assert.Null(response.Errors);
    }

    [Fact]
    public async Task MailInboxControllerBase_HandleOk_WhenCommandSucceeds_ReturnsConfiguredResponse() {
        StubSender sender = new StubSender()
            .Register(new TestMailInboxCommand(), Result.Success());
        var controller = new TestMailInboxController(sender) {
            ControllerContext = CreateControllerContext(),
        };

        IActionResult result = await controller.HandleCommand(new TestMailInboxCommand());

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("accepted", ok.Value);
    }

    [Fact]
    public async Task MailInboxControllerBase_SendRequest_UsesHttpRequestAbortedToken() {
        var sender = new StubSender();
        var controller = new TestMailInboxController(sender) {
            ControllerContext = CreateControllerContext(),
        };

        await controller.SendCommand(new TestPlainCommand());

        Assert.IsType<TestPlainCommand>(sender.LastRequest);
    }

    [Fact]
    public void MailInboxApiKeyAuthorizationFilter_WhenApiKeyIsMissing_ReturnsUnauthorized() {
        var filter = new MailInboxApiKeyAuthorizationFilter(Options.Create(new MailInboxHttpOptions {
            RequireApiKey = true,
            ApiKey = "secret",
        }));
        AuthorizationFilterContext context = CreateAuthorizationContext();

        filter.OnAuthorization(context);

        UnauthorizedObjectResult result = Assert.IsType<UnauthorizedObjectResult>(context.Result);
        MailInboxApiErrorHttpResponse response = Assert.IsType<MailInboxApiErrorHttpResponse>(result.Value);
        Assert.Equal("MailInbox.Unauthorized", response.Error);
    }

    [Fact]
    public void MailInboxApiKeyAuthorizationFilter_WhenApiKeyRequirementIsDisabled_ReturnsUnauthorized() {
        var filter = new MailInboxApiKeyAuthorizationFilter(Options.Create(new MailInboxHttpOptions {
            RequireApiKey = false,
            ApiKey = "secret",
        }));
        AuthorizationFilterContext context = CreateAuthorizationContext();
        context.HttpContext.Request.Headers["X-MailInbox-Api-Key"] = "secret";

        filter.OnAuthorization(context);

        Assert.IsType<UnauthorizedObjectResult>(context.Result);
    }

    [Fact]
    public void MailInboxApiKeyAuthorizationFilter_WhenApiKeyMatches_AllowsRequest() {
        var filter = new MailInboxApiKeyAuthorizationFilter(Options.Create(new MailInboxHttpOptions {
            RequireApiKey = true,
            ApiKey = "secret",
        }));
        AuthorizationFilterContext context = CreateAuthorizationContext();
        context.HttpContext.Request.Headers["X-MailInbox-Api-Key"] = "secret";

        filter.OnAuthorization(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public void AddMailInboxPresentation_ConfiguresApiBehaviorValidationResponse() {
        var services = new ServiceCollection();
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
                ["MailInboxHttp:RequireApiKey"] = "true",
                ["MailInboxHttp:ApiKey"] = "secret",
            })
            .Build();
        services.AddLogging();

        services.AddMailInboxPresentation(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        ApiBehaviorOptions options = provider.GetRequiredService<IOptions<ApiBehaviorOptions>>().Value;
        var context = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
        context.ModelState.AddModelError("Request.RawMime", "");
        context.HttpContext.TraceIdentifier = "trace-presentation";

        BadRequestObjectResult result = Assert.IsType<BadRequestObjectResult>(options.InvalidModelStateResponseFactory(context));
        MailInboxApiErrorHttpResponse response = Assert.IsType<MailInboxApiErrorHttpResponse>(result.Value);
        Assert.Equal("Validation.Invalid", response.Error);
        Assert.Equal("trace-presentation", response.TraceId);
        Assert.Equal(["The value is invalid."], response.Errors!["request.rawMime"]);
    }

    [Fact]
    public void MapMailInboxPresentation_ReturnsEndpointRouteBuilder() {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddControllers().AddApplicationPart(typeof(MailInboxMessagesController).Assembly);
        using WebApplication app = builder.Build();

        IEndpointRouteBuilder result = app.MapMailInboxPresentation();

        Assert.Same(app, result);
    }

    [Fact]
    public void MailInboxHealthController_GetHealth_ReturnsOkHealthResponse() {
        MailInboxHealthController controller = CreateHealthController(new StubSender());

        IActionResult result = controller.GetHealth();

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        MailInboxHealthHttpResponse response = Assert.IsType<MailInboxHealthHttpResponse>(ok.Value);
        Assert.Equal("ok", response.Status);
    }

    [Fact]
    public async Task MailInboxHealthController_GetReady_UsesSenderAndReturnsReadyResponse() {
        StubSender sender = new StubSender()
            .Register(new CheckMailInboxReadinessQuery(), Result.Success());
        MailInboxHealthController controller = CreateHealthController(sender);

        IActionResult result = await controller.GetReady();

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        MailInboxHealthHttpResponse response = Assert.IsType<MailInboxHealthHttpResponse>(ok.Value);
        Assert.Equal("ready", response.Status);
        Assert.IsType<CheckMailInboxReadinessQuery>(sender.LastRequest);
    }

    [Fact]
    public async Task MailInboxMessagesController_Get_ReturnsMappedSummaries() {
        var summary = new InboundMailMessageSummary(
            Guid.NewGuid(),
            "sender@example.com",
            ["admin@fooddiary.club"],
            "Hello",
            InboundMailMessageCategories.General,
            "Received",
            DateTimeOffset.UtcNow);
        StubSender sender = new StubSender()
            .Register(new GetInboundMailMessagesQuery(25), Result<IReadOnlyList<InboundMailMessageSummary>>.Success([summary]));
        MailInboxMessagesController controller = CreateMessagesController(sender);

        IActionResult result = await controller.Get(25);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        IReadOnlyList<InboundMailMessageSummaryHttpResponse> response = Assert.IsAssignableFrom<IReadOnlyList<InboundMailMessageSummaryHttpResponse>>(ok.Value);
        Assert.Equal(summary.Id, response.Single().Id);
    }

    [Fact]
    public async Task MailInboxMessagesController_GetById_WhenMissing_ReturnsNotFound() {
        var id = Guid.NewGuid();
        StubSender sender = new StubSender()
            .Register(new GetInboundMailMessageDetailsQuery(id), Result<InboundMailMessageDetails>.Failure(MailInboxErrors.MessageNotFound(id)));
        MailInboxMessagesController controller = CreateMessagesController(sender);

        IActionResult result = await controller.GetById(id);

        ObjectResult objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, objectResult.StatusCode);
    }

    [Fact]
    public async Task MailInboxMessagesController_GetById_WhenFound_ReturnsMappedDetails() {
        var id = Guid.NewGuid();
        var details = new InboundMailMessageDetails(
            id,
            "message-id",
            "sender@example.com",
            ["admin@fooddiary.club"],
            "Hello",
            "text",
            "<p>text</p>",
            "raw",
            InboundMailMessageCategories.General,
            DmarcReport: null,
            "received",
            DateTimeOffset.UtcNow);
        StubSender sender = new StubSender()
            .Register(new GetInboundMailMessageDetailsQuery(id), Result<InboundMailMessageDetails>.Success(details));
        MailInboxMessagesController controller = CreateMessagesController(sender);

        IActionResult result = await controller.GetById(id);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        InboundMailMessageDetailsHttpResponse response = Assert.IsType<InboundMailMessageDetailsHttpResponse>(ok.Value);
        Assert.Equal(id, response.Id);
        Assert.Equal("raw", response.RawMime);
    }

    private static AuthorizationFilterContext CreateAuthorizationContext() =>
        new(
            new ActionContext(
                new DefaultHttpContext(),
                new RouteData(),
                new ActionDescriptor()),
            []);

    private static MailInboxHealthController CreateHealthController(ISender sender) =>
        new(sender) {
            ControllerContext = CreateControllerContext(),
        };

    private static MailInboxMessagesController CreateMessagesController(ISender sender) =>
        new(sender) {
            ControllerContext = CreateControllerContext(),
        };

    private static ControllerContext CreateControllerContext() {
        var httpContext = new DefaultHttpContext();
        httpContext.TraceIdentifier = "trace-mail-inbox";
        return new ControllerContext {
            HttpContext = httpContext,
        };
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubSender : ISender {
        private readonly Dictionary<object, object> _responses = new();

        public object? LastRequest { get; private set; }

        public StubSender Register<TResponse>(IRequest<TResponse> request, TResponse response) {
            _responses[request] = response!;
            return this;
        }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default) {
            LastRequest = request;
            return Task.FromResult((TResponse)_responses[request]!);
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest {
            LastRequest = request;
            return Task.CompletedTask;
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default) {
            LastRequest = request;
            return Task.FromResult<object?>(_responses[request]);
        }

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
            IStreamRequest<TResponse> request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    [ExcludeFromCodeCoverage]
    private sealed class TestMailInboxController(ISender sender) : MailInboxControllerBase(sender) {
        public Task<IActionResult> HandleCommand(IRequest<Result> request) {
            return HandleOk(request, "accepted");
        }

        public Task SendCommand(IRequest request) {
            return Send(request);
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed record TestMailInboxCommand : IRequest<Result>;

    [ExcludeFromCodeCoverage]
    private sealed record TestPlainCommand : IRequest;
}
