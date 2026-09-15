using FoodDiary.Modules.Identity.Presentation.Security;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FoodDiary.Modules.Identity.Presentation.Filters;

public sealed class AuthenticationCookieResultFilter(RefreshTokenCookieService refreshTokenCookies) : IAsyncResultFilter {
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next) {
        if (context.Result is ObjectResult { Value: AuthenticationHttpResponse authentication }) {
            refreshTokenCookies.Set(context.HttpContext, authentication.RefreshToken);
        }

        await next();
    }
}
