using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Presentation.Api.Extensions;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Web.Api.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Options;

namespace FoodDiary.Web.Api.Extensions;

public sealed class HealthConsentMiddleware(RequestDelegate next, IOptions<PrivacyOptions> options) {
    private static readonly HashSet<string> HealthAssemblies = [with(StringComparer.Ordinal),
        "FoodDiary.Modules.Meals.Presentation", "FoodDiary.Modules.BodyMetrics.Presentation",
        "FoodDiary.Modules.Cycles.Presentation", "FoodDiary.Modules.Hydration.Presentation",
        "FoodDiary.Modules.Fasting.Presentation", "FoodDiary.Modules.Exercises.Presentation",
        "FoodDiary.Modules.WeeklyCheckIn.Presentation", "FoodDiary.Modules.WeeklyGoals.Presentation",
        "FoodDiary.Modules.Wearables.Presentation", "FoodDiary.Modules.Dashboard.Presentation",
        "FoodDiary.Modules.Statistics.Presentation", "FoodDiary.Modules.Tdee.Presentation",
    ];

    public async Task InvokeAsync(HttpContext context, IUserPrivacyStateReader privacy) {
        ControllerActionDescriptor? action = context.GetEndpoint()?.Metadata.GetMetadata<ControllerActionDescriptor>();
        bool healthAction = action is not null && (HealthAssemblies.Contains(action.ControllerTypeInfo.Assembly.GetName().Name ?? string.Empty)
            || (string.Equals(action.ControllerName, "Users", StringComparison.Ordinal)
                && action.ActionName is "UpdateDesiredWeight" or "UpdateDesiredWaist"));
        if (!options.Value.RequireHealthConsent || !healthAction || HttpMethods.IsDelete(context.Request.Method)
            || context.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() is not null
            || context.User.Identity?.IsAuthenticated != true || context.User.GetUserGuid() is not { } userId) {
            await next(context);
            return;
        }

        UserPrivacyStateModel? state = await privacy.GetAsync(new UserId(userId), context.RequestAborted);
        if (state?.HealthAcceptedAtUtc is not null) {
            await next(context);
            return;
        }
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.Headers.CacheControl = "no-store";
        await context.Response.WriteAsJsonAsync(new ApiErrorHttpResponse("Privacy.HealthConsentRequired",
            "Review your health data processing choices in privacy settings.", context.TraceIdentifier), context.RequestAborted);
    }
}
