using FoodDiary.Presentation.Api.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FoodDiary.Modules.Ai.Presentation.Recipes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RecipeVideoAdmissionAttribute : Attribute, IAsyncResourceFilter {
    private static readonly SemaphoreSlim Slots = new(2);

    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next) {
        // Admit before multipart model binding creates temporary upload files.
        if (!await Slots.WaitAsync(0, context.HttpContext.RequestAborted).ConfigureAwait(false)) {
            context.Result = new ObjectResult(new ApiErrorHttpResponse(
                "Ai.VideoBusy", "Video recognition is busy. Try again shortly.", context.HttpContext.TraceIdentifier)) {
                StatusCode = StatusCodes.Status429TooManyRequests,
            };
            return;
        }
        try {
            await next().ConfigureAwait(false);
        } finally {
            Slots.Release();
        }
    }
}
