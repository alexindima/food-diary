using FoodDiary.MailRelay.Presentation.Responses;
using FoodDiary.MailRelay.Presentation.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace FoodDiary.MailRelay.Presentation.Filters;

public sealed class RelayApiKeyAuthorizationFilter(IOptions<MailRelayOptions> relayOptions) : IAuthorizationFilter {
    public void OnAuthorization(AuthorizationFilterContext context) {
        if (RelayRequestAuthorizer.IsAuthorized(context.HttpContext.Request, relayOptions.Value)) {
            return;
        }

        context.Result = new UnauthorizedObjectResult(new MailRelayApiErrorHttpResponse(
            "MailRelay.Unauthorized",
            "A valid relay API key is required.",
            context.HttpContext.TraceIdentifier));
    }
}
