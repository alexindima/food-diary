using FoodDiary.Modules.Billing.Application.Abstractions.Common;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FoodDiary.Modules.Billing.Presentation.Security;

public sealed class TelegramStarsBotAuthorizationFilter(ITelegramStarsBotAuthenticator authenticator) : IAuthorizationFilter {
    public void OnAuthorization(AuthorizationFilterContext context) {
        if (!authenticator.IsAuthenticated(context.HttpContext.Request.Headers["X-Telegram-Bot-Secret"].ToString())) {
            var error = new Error("Billing.InvalidTelegramBotSecret", "Bot authentication failed.", Kind: ErrorKind.Unauthorized);
            context.Result = new ObjectResult(PresentationErrorHttpMapper.MapResponse(error, context.HttpContext.TraceIdentifier)) {
                StatusCode = PresentationErrorHttpMapper.MapStatusCode(error),
            };
        }
    }
}
