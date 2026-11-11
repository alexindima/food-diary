using System.Security.Claims;
using FoodDiary.Modules.Identity.Application.Authentication.Services;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Identity.Contracts.Errors;
using FoodDiary.Modules.Users.Domain.Contracts.Enums;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Http;

namespace FoodDiary.Modules.Identity.Presentation.Security;

public sealed class AdminSessionAuthorizationHandler(PrivilegedSessionService sessions) : IAuthorizationHandler {
    internal sealed record CredentialChangeRequirement : IAuthorizationRequirement;

    public async Task HandleAsync(AuthorizationHandlerContext context) {
        CredentialChangeRequirement? credentialChange = context.Requirements.OfType<CredentialChangeRequirement>().FirstOrDefault();
        if (!context.User.IsInRole(RoleNames.Admin)) {
            if (credentialChange is not null) {
                context.Succeed(credentialChange);
            }
            return;
        }
        if (credentialChange is null && !context.Requirements.OfType<RolesAuthorizationRequirement>()
            .Any(requirement => requirement.AllowedRoles.Contains(RoleNames.Admin, StringComparer.Ordinal))) {
            return;
        }
        CancellationToken cancellationToken = context.Resource is HttpContext http ? http.RequestAborted : CancellationToken.None;
        bool confirmed = Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out Guid userId) &&
            Guid.TryParse(context.User.FindFirstValue(JwtSecurityClaimNames.RefreshSessionId), out Guid sessionId) &&
            await sessions.IsConfirmedAsync(new UserId(userId), sessionId, allowAdminSession: credentialChange is null, cancellationToken);
        if (!confirmed) {
            context.Fail(new AuthorizationFailureReason(this, IdentityErrors.ReauthenticationRequired.Code));
        } else if (credentialChange is not null) {
            context.Succeed(credentialChange);
        }
    }
}
