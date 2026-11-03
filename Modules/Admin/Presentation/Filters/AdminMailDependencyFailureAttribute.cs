using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FoodDiary.Modules.Admin.Presentation.Filters;

[AttributeUsage(AttributeTargets.Class)]
internal sealed class AdminMailDependencyFailureAttribute(string errorCode) : ExceptionFilterAttribute {
    public override void OnException(ExceptionContext context) {
        if (context.HttpContext.RequestAborted.IsCancellationRequested ||
            context.Exception is not (HttpRequestException or OperationCanceledException { InnerException: TimeoutException })) {
            return;
        }

        var error = new Error(errorCode, "The mail service could not complete the request.", ErrorKind.ExternalFailure);
        context.Result = new ObjectResult(PresentationErrorHttpMapper.MapResponse(error, context.HttpContext.TraceIdentifier)) {
            StatusCode = PresentationErrorHttpMapper.MapStatusCode(error),
        };
        context.ExceptionHandled = true;
    }
}
