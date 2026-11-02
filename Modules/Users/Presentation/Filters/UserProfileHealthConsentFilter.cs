using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Presentation.Requests;
using FoodDiary.Presentation.Api.Extensions;
using FoodDiary.Presentation.Api.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FoodDiary.Modules.Users.Presentation.Filters;

public sealed class UserProfileHealthConsentFilter(IUserPrivacyStateReader privacy) : IAsyncActionFilter {
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next) {
        UpdateUserHttpRequest? request = context.ActionArguments.Values.OfType<UpdateUserHttpRequest>().SingleOrDefault();
        if (request is null || !WritesHealthData(request) || context.HttpContext.User.GetUserGuid() is not { } userId) {
            await next();
            return;
        }
        if ((await privacy.GetAsync(new UserId(userId), context.HttpContext.RequestAborted))?.HealthAcceptedAtUtc is not null) {
            await next();
            return;
        }
        context.HttpContext.Response.Headers.CacheControl = "no-store";
        context.Result = new ObjectResult(new ApiErrorHttpResponse("Privacy.HealthConsentRequired",
            "Review your health data processing choices in privacy settings.", context.HttpContext.TraceIdentifier)) {
            StatusCode = StatusCodes.Status403Forbidden,
        };
    }

    private static bool WritesHealthData(UpdateUserHttpRequest request) => request.BirthDate is not null || request.Gender is not null
        || request.WeightKg is not null || request.HeightCm is not null || request.ActivityLevel is not null
        || request.StepGoal is not null || request.HydrationGoal is not null;
}
