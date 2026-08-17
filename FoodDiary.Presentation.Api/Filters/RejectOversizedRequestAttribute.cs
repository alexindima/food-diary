using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FoodDiary.Presentation.Api.Filters;

[AttributeUsage(AttributeTargets.Method)]
public sealed class RejectOversizedRequestAttribute : Attribute, IResourceFilter, IOrderedFilter {
    public RejectOversizedRequestAttribute(long maxBytes) {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxBytes);
        MaxBytes = maxBytes;
    }

    public long MaxBytes { get; }

    public int Order => int.MinValue;

    public void OnResourceExecuting(ResourceExecutingContext context) {
        if (context.HttpContext.Request.ContentLength > MaxBytes) {
            context.Result = new StatusCodeResult(StatusCodes.Status413PayloadTooLarge);
        }
    }

    public void OnResourceExecuted(ResourceExecutedContext context) {
    }
}
