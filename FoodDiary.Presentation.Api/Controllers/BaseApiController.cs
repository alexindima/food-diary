using System.Diagnostics;
using Asp.Versioning;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Export.Models;
using FoodDiary.Presentation.Api.Extensions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FoodDiary.Presentation.Api.Controllers;

[ApiVersion("1.0")]
public abstract class BaseApiController(ISender mediator) : ControllerBase {
    protected ISender Mediator { get; } = mediator;

    protected Task Send(IRequest request) {
        return Mediator.Send(request, HttpContext.RequestAborted);
    }

    protected Task<TResponse> Send<TResponse>(IRequest<TResponse> request) {
        return Mediator.Send(request, HttpContext.RequestAborted);
    }

    protected async Task<IActionResult> HandleOk<TResponse, THttpResponse>(
        IRequest<Result<TResponse>> request,
        Func<TResponse, THttpResponse> map) {
        var result = await Send(request);
        return result.ToOkActionResult(this, map);
    }

    protected async Task<IActionResult> HandleCreated<TResponse, THttpResponse>(
        IRequest<Result<TResponse>> request,
        string actionName,
        Func<TResponse, object?> routeValues,
        Func<TResponse, THttpResponse> map) {
        var result = await Send(request);
        return result.ToCreatedAtActionResult(this, actionName, routeValues, map);
    }

    protected async Task<IActionResult> HandleCreated<TResponse, THttpResponse>(
        IRequest<Result<TResponse>> request,
        Func<TResponse, THttpResponse> map) {
        var result = await Send(request);
        return result.ToCreatedActionResult(this, map);
    }

    protected async Task<IActionResult> HandleNoContent(IRequest<Result> request) {
        var result = await Send(request);
        return result.ToNoContentActionResult(this);
    }

    protected async Task<IActionResult> HandleFile(IRequest<Result<FileExportResult>> request) {
        var result = await Send(request);
        return result.ToFileActionResult(this);
    }

    protected async Task<IActionResult> HandleObservedOk<TResponse, THttpResponse>(
        IRequest<Result<TResponse>> request,
        Func<TResponse, THttpResponse> map,
        ILogger logger,
        string operationName,
        Guid? userId = null) {
        using var observation = BeginObservation(operationName, userId);
        var result = await Send(request);
        CompleteObservation(observation, logger, result.IsSuccess, result.IsSuccess ? null : result.Error);
        return result.ToOkActionResult(this, map);
    }

    protected async Task<IActionResult> HandleObservedCreated<TResponse, THttpResponse>(
        IRequest<Result<TResponse>> request,
        string actionName,
        Func<TResponse, object?> routeValues,
        Func<TResponse, THttpResponse> map,
        ILogger logger,
        string operationName,
        Guid? userId = null) {
        using var observation = BeginObservation(operationName, userId);
        var result = await Send(request);
        CompleteObservation(observation, logger, result.IsSuccess, result.IsSuccess ? null : result.Error);
        return result.ToCreatedAtActionResult(this, actionName, routeValues, map);
    }

    protected async Task<IActionResult> HandleObservedNoContent(
        IRequest<Result> request,
        ILogger logger,
        string operationName,
        Guid? userId = null) {
        using var observation = BeginObservation(operationName, userId);
        var result = await Send(request);
        CompleteObservation(observation, logger, result.IsSuccess, result.IsSuccess ? null : result.Error);
        return result.ToNoContentActionResult(this);
    }

    private PresentationOperationObservation BeginObservation(string operationName, Guid? userId) {
        var stopwatch = Stopwatch.StartNew();
        var activity = PresentationApiTelemetry.ActivitySource.StartActivity(operationName, ActivityKind.Internal);
        var feature = ResolveFeatureName();
        var controllerName = GetType().Name;
        var route = HttpContext.GetEndpoint()?.DisplayName;

        activity?.SetTag("fooddiary.presentation.feature", feature);
        activity?.SetTag("fooddiary.presentation.controller", controllerName);
        activity?.SetTag("fooddiary.presentation.operation", operationName);
        activity?.SetTag("http.route", route);
        if (userId.HasValue) {
            activity?.SetTag("enduser.id", userId.Value);
        }

        return new PresentationOperationObservation(operationName, feature, controllerName, route, userId, stopwatch, activity);
    }

    private void CompleteObservation(
        PresentationOperationObservation observation,
        ILogger logger,
        bool isSuccess,
        Error? error) {
        observation.Stopwatch.Stop();
        var outcome = isSuccess ? "success" : "failure";

        observation.Activity?.SetTag("fooddiary.presentation.outcome", outcome);
        observation.Activity?.SetTag("fooddiary.presentation.duration_ms", observation.Stopwatch.Elapsed.TotalMilliseconds);

        PresentationApiTelemetry.OperationCounter.Add(
            1,
            new KeyValuePair<string, object?>("fooddiary.presentation.feature", observation.Feature),
            new KeyValuePair<string, object?>("fooddiary.presentation.controller", observation.ControllerName),
            new KeyValuePair<string, object?>("fooddiary.presentation.operation", observation.OperationName),
            new KeyValuePair<string, object?>("fooddiary.presentation.outcome", outcome));
        PresentationApiTelemetry.OperationDuration.Record(
            observation.Stopwatch.Elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("fooddiary.presentation.feature", observation.Feature),
            new KeyValuePair<string, object?>("fooddiary.presentation.controller", observation.ControllerName),
            new KeyValuePair<string, object?>("fooddiary.presentation.operation", observation.OperationName),
            new KeyValuePair<string, object?>("fooddiary.presentation.outcome", outcome));

        if (isSuccess) {
            observation.Activity?.SetStatus(ActivityStatusCode.Ok);
            logger.LogInformation(
                "Presentation operation {OperationName} in {Feature}/{Controller} completed successfully in {ElapsedMs} ms",
                observation.OperationName,
                observation.Feature,
                observation.ControllerName,
                observation.Stopwatch.Elapsed.TotalMilliseconds);
            return;
        }

        if (error is not null) {
            observation.Activity?.SetStatus(ActivityStatusCode.Error, error.Message);
            observation.Activity?.SetTag("error.type", error.Code);
            observation.Activity?.SetTag("error.message", error.Message);
            PresentationApiTelemetry.OperationFailureCounter.Add(
                1,
                new KeyValuePair<string, object?>("fooddiary.presentation.feature", observation.Feature),
                new KeyValuePair<string, object?>("fooddiary.presentation.controller", observation.ControllerName),
                new KeyValuePair<string, object?>("fooddiary.presentation.operation", observation.OperationName),
                new KeyValuePair<string, object?>("error.code", error.Code));
            logger.LogWarning(
                "Presentation operation {OperationName} in {Feature}/{Controller} failed with {ErrorCode} in {ElapsedMs} ms",
                observation.OperationName,
                observation.Feature,
                observation.ControllerName,
                error.Code,
                observation.Stopwatch.Elapsed.TotalMilliseconds);
        }
    }

    private string ResolveFeatureName() {
        var namespaceValue = GetType().Namespace;
        if (string.IsNullOrWhiteSpace(namespaceValue)) {
            return "Unknown";
        }

        var segments = namespaceValue.Split('.');
        var featuresIndex = Array.IndexOf(segments, "Features");
        return featuresIndex >= 0 && featuresIndex < segments.Length - 1
            ? segments[featuresIndex + 1]
            : "Unknown";
    }

    private sealed record PresentationOperationObservation(
        string OperationName,
        string Feature,
        string ControllerName,
        string? Route,
        Guid? UserId,
        Stopwatch Stopwatch,
        Activity? Activity) : IDisposable {
        public void Dispose() {
            Activity?.Dispose();
        }
    }
}
