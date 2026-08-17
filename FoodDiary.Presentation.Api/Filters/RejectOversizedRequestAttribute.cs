using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using FoodDiary.Presentation.Api.Responses;

namespace FoodDiary.Presentation.Api.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RejectOversizedRequestAttribute : Attribute, IResourceFilter, IOrderedFilter {
    public RejectOversizedRequestAttribute(long maxBytes) {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxBytes);
        MaxBytes = maxBytes;
    }

    public long MaxBytes { get; }

    public int Order => int.MinValue + 100;

    public void OnResourceExecuting(ResourceExecutingContext context) {
        if (context.HttpContext.Request.ContentLength > MaxBytes) {
            context.Result = new ObjectResult(new ApiErrorHttpResponse(
                "Request.PayloadTooLarge",
                "The request payload is too large.",
                context.HttpContext.TraceIdentifier)) {
                StatusCode = StatusCodes.Status413PayloadTooLarge,
            };
        }
    }

    public void OnResourceExecuted(ResourceExecutedContext context) {
    }
}
