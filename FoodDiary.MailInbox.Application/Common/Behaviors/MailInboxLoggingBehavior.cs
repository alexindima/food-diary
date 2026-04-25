using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using MailInboxResult = FoodDiary.MailInbox.Application.Common.Result.Result;

namespace FoodDiary.MailInbox.Application.Common.Behaviors;

public sealed class MailInboxLoggingBehavior<TRequest, TResponse>(
    ILogger<MailInboxLoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : MailInboxResult {
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken) {
        var requestName = typeof(TRequest).Name;
        logger.LogDebug("Handling {RequestName}", requestName);

        var stopwatch = Stopwatch.StartNew();
        try {
            var response = await next(cancellationToken);
            stopwatch.Stop();

            if (response.IsFailure) {
                logger.LogWarning(
                    "Handled {RequestName} with error {ErrorCode}: {ErrorMessage} ({ElapsedMs}ms)",
                    requestName,
                    response.Error?.Code,
                    response.Error?.Message,
                    stopwatch.ElapsedMilliseconds);
            } else {
                logger.LogDebug("Handled {RequestName} successfully ({ElapsedMs}ms)", requestName, stopwatch.ElapsedMilliseconds);
            }

            return response;
        } catch (Exception ex) {
            stopwatch.Stop();
            logger.LogError(ex, "Unhandled exception in {RequestName} ({ElapsedMs}ms)", requestName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
