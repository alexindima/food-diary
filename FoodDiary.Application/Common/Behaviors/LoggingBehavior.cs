using System.Diagnostics;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Mediator;
using Microsoft.Extensions.Logging;

namespace FoodDiary.Application.Common.Behaviors;

public class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result {

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken) {
        string requestName = typeof(TRequest).Name;

        logger.LogDebug("Handling {RequestName}", requestName);

        var stopwatch = Stopwatch.StartNew();

        try {
            TResponse response = await next(cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();

            if (response.IsFailure) {
                LogFailure(requestName, response.Error, stopwatch.ElapsedMilliseconds);
            } else {
                logger.LogDebug(
                    "Handled {RequestName} successfully ({ElapsedMs}ms)",
                    requestName, stopwatch.ElapsedMilliseconds);
            }

            return response;
        } catch (Exception ex) {
            stopwatch.Stop();
            logger.LogError(ex,
                "Unhandled exception in {RequestName} ({ElapsedMs}ms)",
                requestName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    private void LogFailure(string requestName, Error error, long elapsedMs) {
        LogLevel level = ResolveFailureLogLevel(error);
        logger.Log(
            level,
            "Handled {RequestName} with error {ErrorCode}: {ErrorMessage} ({ElapsedMs}ms)",
            requestName,
            error.Code,
            error.Message,
            elapsedMs);
    }

    private static LogLevel ResolveFailureLogLevel(Error error) {
        ErrorKind? kind = error.Kind ?? ErrorKindResolver.Resolve(error.Code);
        return kind is ErrorKind.Validation or ErrorKind.Unauthorized or ErrorKind.Forbidden
            ? LogLevel.Information
            : LogLevel.Warning;
    }
}
