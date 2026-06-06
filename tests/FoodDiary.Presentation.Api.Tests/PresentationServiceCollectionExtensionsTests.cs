using FoodDiary.Application.Authentication.Common;
using FoodDiary.Application.Abstractions.Fasting.Common;
using FoodDiary.Presentation.Api.Extensions;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Presentation.Api.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class PresentationServiceCollectionExtensionsTests {
    [Fact]
    public void AddPresentationApi_RegistersPresentationServices() {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddPresentationApi();
        using ServiceProvider provider = services.BuildServiceProvider();

        IEmailVerificationNotifier emailVerificationNotifier = provider.GetRequiredService<IEmailVerificationNotifier>();
        IUserIdProvider userIdProvider = provider.GetRequiredService<IUserIdProvider>();

        Assert.IsType<EmailVerificationNotifier>(emailVerificationNotifier);
        Assert.IsType<UserIdProvider>(userIdProvider);
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IActionDescriptorCollectionProvider));
    }

    [Fact]
    public void AddPresentationApi_ConfiguresInvalidModelStateFactory_ToReturnApiErrorResponse() {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddOptions();
        services.AddPresentationApi();
        using ServiceProvider provider = services.BuildServiceProvider();

        ApiBehaviorOptions apiBehaviorOptions = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ApiBehaviorOptions>>().Value;
        var httpContext = new DefaultHttpContext {
            RequestServices = provider,
            TraceIdentifier = "trace-123",
        };

        var actionContext = new ActionContext {
            HttpContext = httpContext,
        };
        actionContext.ModelState.AddModelError("email", "Email is required.");

        IActionResult result = apiBehaviorOptions.InvalidModelStateResponseFactory(actionContext);

        BadRequestObjectResult badRequest = Assert.IsType<BadRequestObjectResult>(result);
        ApiErrorHttpResponse response = Assert.IsType<ApiErrorHttpResponse>(badRequest.Value);
        Assert.Equal("Validation.Invalid", response.Error);
        Assert.Equal("One or more validation errors occurred.", response.Message);
        Assert.Equal("trace-123", response.TraceId);
        Assert.NotNull(response.Errors);
        Assert.True(response.Errors.TryGetValue("email", out string[]? errors));
        Assert.Equal(["Email is required."], errors);
    }

    [Fact]
    public void MapPresentationApi_MapsEmailVerificationHub() {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddCors(options => options.AddPolicy("TestCors", policy => policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()));
        builder.Services.AddAuthorization();
        builder.Services.AddDistributedMemoryCache();
        builder.Services.AddScoped<IFastingTelemetryEventRepository, StubFastingTelemetryEventRepository>();
        builder.Services.AddSingleton<TimeProvider, StubDateTimeProvider>();
        builder.Services.AddPresentationApi();

        WebApplication app = builder.Build();

        app.MapPresentationApi("TestCors");

        RouteEndpoint[] endpoints = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(dataSource => dataSource.Endpoints)
            .OfType<RouteEndpoint>()
            .ToArray();

        Assert.Contains(endpoints, endpoint => string.Equals(endpoint.RoutePattern.RawText, "/hubs/email-verification", StringComparison.Ordinal));
        Assert.Contains(endpoints, endpoint => string.Equals(endpoint.RoutePattern.RawText, "/hubs/email-verification/negotiate", StringComparison.Ordinal));
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubFastingTelemetryEventRepository : IFastingTelemetryEventRepository {
        public Task AddAsync(FastingTelemetryEventRecord record, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<FastingTelemetryEventRecord>> GetSinceAsync(DateTime sinceUtc, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<FastingTelemetryEventRecord>>([]);
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubDateTimeProvider : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
    }
}
