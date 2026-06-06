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
                logger.LogWarning(
                    "Handled {RequestName} with error {ErrorCode}: {ErrorMessage} ({ElapsedMs}ms)",
                    requestName, response.Error.Code, response.Error.Message, stopwatch.ElapsedMilliseconds);
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
}
