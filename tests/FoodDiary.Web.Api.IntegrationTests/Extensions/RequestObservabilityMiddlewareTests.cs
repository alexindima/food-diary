using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Security.Claims;
using FoodDiary.Web.Api.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodDiary.Web.Api.IntegrationTests.Extensions;

public sealed class RequestObservabilityMiddlewareTests {
    [Fact]
    public async Task Middleware_PublishesActivity_ForRequest() {
        Activity? capturedActivity = null;

        using var listener = new ActivityListener {
            ShouldListenTo = source => string.Equals(source.Name, ApiTelemetry.TelemetryName, StringComparison.Ordinal),
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            SampleUsingParentId = static (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity => {
                if (string.Equals(activity.GetTagItem("url.path")?.ToString(), "/telemetry/activity", StringComparison.Ordinal)) {
                    capturedActivity = activity;
                }
            },
        };
        ActivitySource.AddActivityListener(listener);

        var middleware = new RequestObservabilityMiddleware(
            next: context => {
                context.Response.StatusCode = StatusCodes.Status204NoContent;
                return Task.CompletedTask;
            },
            logger: NullLogger<RequestObservabilityMiddleware>.Instance);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = HttpMethods.Get;
        httpContext.Request.Path = "/telemetry/activity";

        await middleware.InvokeAsync(httpContext);

        Assert.NotNull(capturedActivity);
        Assert.Equal("fooddiary.http.request", capturedActivity!.OperationName);
        Assert.Equal("/telemetry/activity", capturedActivity.GetTagItem("url.path"));
        Assert.Equal(StatusCodes.Status204NoContent, capturedActivity.GetTagItem("http.response.status_code"));
    }

    [Fact]
    public async Task Middleware_RecordsRequestDurationMetric() {
        double? duration = null;

        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) => {
            if (string.Equals(instrument.Meter.Name, ApiTelemetry.TelemetryName, StringComparison.Ordinal) &&
string.Equals(instrument.Name, "fooddiary.api.request.duration", StringComparison.Ordinal)) {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<double>((_, measurement, _, _) => duration = measurement);
        listener.Start();

        var middleware = new RequestObservabilityMiddleware(
            next: context => {
                context.Response.StatusCode = StatusCodes.Status200OK;
                return Task.CompletedTask;
            },
            logger: NullLogger<RequestObservabilityMiddleware>.Instance);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = HttpMethods.Get;
        httpContext.Request.Path = "/telemetry/metrics";

        await middleware.InvokeAsync(httpContext);
        listener.RecordObservableInstruments();

        Assert.NotNull(duration);
        Assert.True(duration >= 0);
    }

    [Fact]
    public async Task Middleware_RedactsUserTelemetry_ForSensitiveAuthRoutes() {
        Activity? capturedActivity = null;

        using var listener = new ActivityListener {
            ShouldListenTo = source => string.Equals(source.Name, ApiTelemetry.TelemetryName, StringComparison.Ordinal),
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            SampleUsingParentId = static (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity => {
                if (string.Equals(activity.GetTagItem("url.path")?.ToString(), "/api/v1/auth/*", StringComparison.Ordinal)) {
                    capturedActivity = activity;
                }
            },
        };
        ActivitySource.AddActivityListener(listener);

        var middleware = new RequestObservabilityMiddleware(
            next: context => {
                context.Response.StatusCode = StatusCodes.Status200OK;
                return Task.CompletedTask;
            },
            logger: NullLogger<RequestObservabilityMiddleware>.Instance);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = HttpMethods.Post;
        httpContext.Request.Path = "/api/v1/auth/login";
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
        ], "test"));

        await middleware.InvokeAsync(httpContext);

        Assert.NotNull(capturedActivity);
        Assert.Equal("/api/v1/auth/*", capturedActivity!.GetTagItem("url.path"));
        Assert.Equal("auth", capturedActivity.GetTagItem("fooddiary.request.sensitivity"));
        Assert.Null(capturedActivity.GetTagItem("enduser.id"));
    }

    [Theory]
    [InlineData("/api/v1/auth/admin-sso/token", "/api/v1/auth/admin-sso/*", "auth-admin-sso")]
    [InlineData("/api/v1/auth/telegram/login", "/api/v1/auth/telegram/*", "auth-telegram")]
    [InlineData("/hubs/email-verification", "/hubs/email-verification", "signalr-auth")]
    public async Task Middleware_RedactsUserTelemetry_ForSensitiveNonDefaultRoutes(
        string requestPath,
        string expectedPathLabel,
        string expectedSensitivity) {
        Activity? capturedActivity = null;

        using var listener = new ActivityListener {
            ShouldListenTo = source => string.Equals(source.Name, ApiTelemetry.TelemetryName, StringComparison.Ordinal),
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            SampleUsingParentId = static (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity => {
                if (string.Equals(activity.GetTagItem("url.path")?.ToString(), expectedPathLabel, StringComparison.Ordinal)) {
                    capturedActivity = activity;
                }
            },
        };
        ActivitySource.AddActivityListener(listener);

        var middleware = new RequestObservabilityMiddleware(
            next: context => {
                context.Response.StatusCode = StatusCodes.Status200OK;
                return Task.CompletedTask;
            },
            logger: NullLogger<RequestObservabilityMiddleware>.Instance);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = HttpMethods.Post;
        httpContext.Request.Path = requestPath;
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
        ], "test"));

        await middleware.InvokeAsync(httpContext);

        Assert.NotNull(capturedActivity);
        Assert.Equal(expectedSensitivity, capturedActivity!.GetTagItem("fooddiary.request.sensitivity"));
        Assert.Null(capturedActivity.GetTagItem("enduser.id"));
    }

    [Fact]
    public async Task Middleware_UsesAnonymousUserTelemetry_ForStandardRouteWithoutUserIdClaim() {
        Activity? capturedActivity = null;

        using var listener = new ActivityListener {
            ShouldListenTo = source => string.Equals(source.Name, ApiTelemetry.TelemetryName, StringComparison.Ordinal),
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            SampleUsingParentId = static (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity => {
                if (string.Equals(activity.GetTagItem("url.path")?.ToString(), "/api/v1/dashboard", StringComparison.Ordinal)) {
                    capturedActivity = activity;
                }
            },
        };
        ActivitySource.AddActivityListener(listener);

        var middleware = new RequestObservabilityMiddleware(
            next: context => {
                context.Response.StatusCode = StatusCodes.Status200OK;
                return Task.CompletedTask;
            },
            logger: NullLogger<RequestObservabilityMiddleware>.Instance);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = HttpMethods.Get;
        httpContext.Request.Path = "/api/v1/dashboard";

        await middleware.InvokeAsync(httpContext);

        Assert.NotNull(capturedActivity);
        Assert.Equal("standard", capturedActivity!.GetTagItem("fooddiary.request.sensitivity"));
        Assert.Equal("anonymous", capturedActivity.GetTagItem("enduser.id"));
    }

    [Fact]
    public async Task Middleware_RecordsBusinessFlowMetric_ForSuccessfulAuthRegister() {
        long? measurement = null;
        string? flow = null;
        string? outcome = null;

        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) => {
            if (string.Equals(instrument.Meter.Name, ApiTelemetry.TelemetryName, StringComparison.Ordinal) &&
string.Equals(instrument.Name, "fooddiary.api.business_flow.events", StringComparison.Ordinal)) {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((_, value, tags, _) => {
            measurement = value;
            flow = GetTagValue(tags, "fooddiary.business_flow");
            outcome = GetTagValue(tags, "fooddiary.business_outcome");
        });
        listener.Start();

        var middleware = new RequestObservabilityMiddleware(
            next: context => {
                context.Response.StatusCode = StatusCodes.Status200OK;
                return Task.CompletedTask;
            },
            logger: NullLogger<RequestObservabilityMiddleware>.Instance);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = HttpMethods.Post;
        httpContext.Request.Path = "/api/v1/auth/register";

        await middleware.InvokeAsync(httpContext);

        Assert.Equal(1, measurement);
        Assert.Equal("auth.register", flow);
        Assert.Equal("success", outcome);
    }

    [Fact]
    public async Task Middleware_RecordsBusinessFlowMetric_ForClientErrorImageUpload() {
        long? measurement = null;
        string? flow = null;
        string? outcome = null;

        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) => {
            if (string.Equals(instrument.Meter.Name, ApiTelemetry.TelemetryName, StringComparison.Ordinal) &&
string.Equals(instrument.Name, "fooddiary.api.business_flow.events", StringComparison.Ordinal)) {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((_, value, tags, _) => {
            measurement = value;
            flow = GetTagValue(tags, "fooddiary.business_flow");
            outcome = GetTagValue(tags, "fooddiary.business_outcome");
        });
        listener.Start();

        var middleware = new RequestObservabilityMiddleware(
            next: context => {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return Task.CompletedTask;
            },
            logger: NullLogger<RequestObservabilityMiddleware>.Instance);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = HttpMethods.Post;
        httpContext.Request.Path = "/api/v1/images/upload-url";

        await middleware.InvokeAsync(httpContext);

        Assert.Equal(1, measurement);
        Assert.Equal("images.upload-url", flow);
        Assert.Equal("client_error", outcome);
    }

    [Theory]
    [InlineData("POST", "/api/v1/auth/login", "auth.login", StatusCodes.Status500InternalServerError, "server_error")]
    [InlineData("POST", "/api/v1/auth/refresh", "auth.refresh", StatusCodes.Status200OK, "success")]
    [InlineData("POST", "/api/v1/auth/restore", "auth.restore", StatusCodes.Status200OK, "success")]
    [InlineData("POST", "/api/v1/auth/password-reset/request", "auth.password-reset.request", StatusCodes.Status200OK, "success")]
    [InlineData("POST", "/api/v1/auth/password-reset/confirm", "auth.password-reset.confirm", StatusCodes.Status200OK, "success")]
    [InlineData("POST", "/api/v1/auth/verify-email", "auth.verify-email", StatusCodes.Status200OK, "success")]
    [InlineData("POST", "/api/v1/auth/verify-email/resend", "auth.verify-email.resend", StatusCodes.Status200OK, "success")]
    [InlineData("DELETE", "/api/v1/images/image-id", "images.delete", StatusCodes.Status204NoContent, "success")]
    [InlineData("DELETE", "/api/v1/users", "users.delete", StatusCodes.Status204NoContent, "success")]
    public async Task Middleware_RecordsBusinessFlowMetric_ForKnownFlows(
        string method,
        string requestPath,
        string expectedFlow,
        int statusCode,
        string expectedOutcome) {
        long? measurement = null;
        string? flow = null;
        string? outcome = null;

        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) => {
            if (string.Equals(instrument.Meter.Name, ApiTelemetry.TelemetryName, StringComparison.Ordinal) &&
string.Equals(instrument.Name, "fooddiary.api.business_flow.events", StringComparison.Ordinal)) {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((_, value, tags, _) => {
            measurement = value;
            flow = GetTagValue(tags, "fooddiary.business_flow");
            outcome = GetTagValue(tags, "fooddiary.business_outcome");
        });
        listener.Start();

        var middleware = new RequestObservabilityMiddleware(
            next: context => {
                context.Response.StatusCode = statusCode;
                return Task.CompletedTask;
            },
            logger: NullLogger<RequestObservabilityMiddleware>.Instance);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = method;
        httpContext.Request.Path = requestPath;

        await middleware.InvokeAsync(httpContext);

        Assert.Equal(1, measurement);
        Assert.Equal(expectedFlow, flow);
        Assert.Equal(expectedOutcome, outcome);
    }

    [Fact]
    public async Task Middleware_RecordsOutputCacheMetric_ForCacheHit() {
        long? measurement = null;
        string? policy = null;
        string? outcome = null;

        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) => {
            if (string.Equals(instrument.Meter.Name, ApiTelemetry.TelemetryName, StringComparison.Ordinal) &&
string.Equals(instrument.Name, "fooddiary.api.output_cache.events", StringComparison.Ordinal)) {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((_, value, tags, _) => {
            measurement = value;
            policy = GetTagValue(tags, "fooddiary.output_cache.policy");
            outcome = GetTagValue(tags, "fooddiary.output_cache.outcome");
        });
        listener.Start();

        var middleware = new RequestObservabilityMiddleware(
            next: context => {
                context.Response.StatusCode = StatusCodes.Status200OK;
                context.Response.Headers.Append("Age", "5");
                return Task.CompletedTask;
            },
            logger: NullLogger<RequestObservabilityMiddleware>.Instance);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = HttpMethods.Get;
        httpContext.Request.Path = "/api/v1/dashboard";
        httpContext.SetEndpoint(new Endpoint(
            static _ => Task.CompletedTask,
            new EndpointMetadataCollection(new OutputCacheAttribute {
                PolicyName = "PresentationUserScopedCache"
            }),
            "dashboard"));

        await middleware.InvokeAsync(httpContext);

        Assert.Equal(1, measurement);
        Assert.Equal("PresentationUserScopedCache", policy);
        Assert.Equal("hit", outcome);
    }

    [Fact]
    public async Task Middleware_RecordsOutputCacheMetric_ForCacheMiss() {
        long? measurement = null;
        string? policy = null;
        string? outcome = null;

        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) => {
            if (string.Equals(instrument.Meter.Name, ApiTelemetry.TelemetryName, StringComparison.Ordinal) &&
string.Equals(instrument.Name, "fooddiary.api.output_cache.events", StringComparison.Ordinal)) {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((_, value, tags, _) => {
            measurement = value;
            policy = GetTagValue(tags, "fooddiary.output_cache.policy");
            outcome = GetTagValue(tags, "fooddiary.output_cache.outcome");
        });
        listener.Start();

        var middleware = new RequestObservabilityMiddleware(
            next: context => {
                context.Response.StatusCode = StatusCodes.Status200OK;
                return Task.CompletedTask;
            },
            logger: NullLogger<RequestObservabilityMiddleware>.Instance);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = HttpMethods.Get;
        httpContext.Request.Path = "/api/v1/admin/ai-usage";
        httpContext.SetEndpoint(new Endpoint(
            static _ => Task.CompletedTask,
            new EndpointMetadataCollection(new OutputCacheAttribute {
                PolicyName = "PresentationAdminAiUsageCache"
            }),
            "admin-ai-usage"));

        await middleware.InvokeAsync(httpContext);

        Assert.Equal(1, measurement);
        Assert.Equal("PresentationAdminAiUsageCache", policy);
        Assert.Equal("miss", outcome);
    }

    private static string? GetTagValue(ReadOnlySpan<KeyValuePair<string, object?>> tags, string key) {
        foreach (var tag in tags) {
            if (string.Equals(tag.Key, key, StringComparison.Ordinal)) {
                return tag.Value?.ToString();
            }
        }

        return null;
    }
}
