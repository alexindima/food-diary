using FoodDiary.MailInbox.Presentation.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace FoodDiary.MailInbox.Presentation.Filters;

public sealed class MailInboxExceptionFilter(ILogger<MailInboxExceptionFilter> logger) : IExceptionFilter {
    public void OnException(ExceptionContext context) {
        if (context.Exception is OperationCanceledException && context.HttpContext.RequestAborted.IsCancellationRequested) {
            return;
        }

        logger.LogError(
            "Unhandled MailInbox HTTP request failure of type {ExceptionType}. Trace ID: {TraceId}",
            context.Exception.GetType().Name,
            context.HttpContext.TraceIdentifier);
        context.Result = new ObjectResult(new MailInboxApiErrorHttpResponse(
            "MailInbox.Internal",
            "An unexpected error occurred.",
            context.HttpContext.TraceIdentifier)) {
            StatusCode = StatusCodes.Status500InternalServerError,
        };
        context.ExceptionHandled = true;
    }
}
