using System.Globalization;
using FoodDiary.Presentation.Api.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FoodDiary.Modules.Identity.Presentation.Features.Auth;

[AttributeUsage(AttributeTargets.Method)]
public sealed class PasswordLoginRetryAfterAttribute : ResultFilterAttribute {
    public override void OnResultExecuting(ResultExecutingContext context) {
        if (context.Result is ObjectResult {
            StatusCode: StatusCodes.Status429TooManyRequests,
            Value: ApiErrorHttpResponse { Error: "RateLimit.Exceeded", Errors: { } details } response,
        } result && details.TryGetValue("retryAfterSeconds", out string[]? values) && values.Length == 1 &&
            int.TryParse(values[0], NumberStyles.None, CultureInfo.InvariantCulture, out int seconds) && seconds > 0) {
            context.HttpContext.Response.Headers.RetryAfter = seconds.ToString(CultureInfo.InvariantCulture);
            result.Value = response with { Errors = null };
        }
    }
}
