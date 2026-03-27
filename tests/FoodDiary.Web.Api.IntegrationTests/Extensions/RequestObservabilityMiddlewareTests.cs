using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Security.Claims;
using FoodDiary.Web.Api.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodDiary.Web.Api.IntegrationTests.Extensions;

public sealed class RequestObservabilityMiddlewareTests {
    [Fact]
    public async Task Middleware_PublishesActivity_ForRequest() {
        Activity? capturedActivity = null;

        using var listener = new ActivityListener {
            ShouldListenTo = source => source.Name == ApiTelemetry.TelemetryName,
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            SampleUsingParentId = static (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData,
            ActivityStarted = activity => capturedActivity = activity,
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
            if (instrument.Meter.Name == ApiTelemetry.TelemetryName &&
                instrument.Name == "fooddiary.api.request.duration") {
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
            ShouldListenTo = source => source.Name == ApiTelemetry.TelemetryName,
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            SampleUsingParentId = static (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData,
            ActivityStarted = activity => capturedActivity = activity,
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
        httpContext.Request.Path = "/api/auth/login";
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
        ], "test"));

        await middleware.InvokeAsync(httpContext);

        Assert.NotNull(capturedActivity);
        Assert.Equal("/api/auth/*", capturedActivity!.GetTagItem("url.path"));
        Assert.Equal("auth", capturedActivity.GetTagItem("fooddiary.request.sensitivity"));
        Assert.Null(capturedActivity.GetTagItem("enduser.id"));
    }
}
