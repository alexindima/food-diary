using FoodDiary.Presentation.Api.Features.Auth.Responses;
using FoodDiary.Presentation.Api.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FoodDiary.Presentation.Api.Filters;

public sealed class AuthenticationCookieResultFilter(RefreshTokenCookieService refreshTokenCookies) : IAsyncResultFilter {
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next) {
        if (context.Result is ObjectResult { Value: AuthenticationHttpResponse authentication }) {
            refreshTokenCookies.Set(context.HttpContext, authentication.RefreshToken);
        }

        await next();
    }
}
