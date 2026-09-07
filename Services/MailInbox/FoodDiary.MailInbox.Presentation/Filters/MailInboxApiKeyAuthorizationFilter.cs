using FoodDiary.MailInbox.Presentation.Options;
using FoodDiary.MailInbox.Presentation.Responses;
using FoodDiary.MailInbox.Presentation.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace FoodDiary.MailInbox.Presentation.Filters;

public sealed class MailInboxApiKeyAuthorizationFilter(IOptions<MailInboxHttpOptions> options) : IAuthorizationFilter {
    public void OnAuthorization(AuthorizationFilterContext context) {
        RequireMailInboxPermissionAttribute? requirement = context.ActionDescriptor.EndpointMetadata
            .OfType<RequireMailInboxPermissionAttribute>()
            .SingleOrDefault();
        if (requirement is not null &&
            MailInboxRequestAuthorizer.IsAuthorized(
                context.HttpContext.Request,
                options.Value,
                requirement.Permission)) {
            return;
        }

        context.Result = new UnauthorizedObjectResult(new MailInboxApiErrorHttpResponse(
            "MailInbox.Unauthorized",
            "A valid mail inbox API key is required.",
            context.HttpContext.TraceIdentifier));
    }
}
