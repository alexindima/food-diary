using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;
using System.Security.Claims;

namespace FoodDiary.Web.Api.Extensions;

internal static class JwtTokenUseValidator {
    internal static bool IsAccessToken(ClaimsPrincipal? principal) =>
        string.Equals(
            principal?.FindFirst(JwtTokenUseClaimNames.ClaimType)?.Value,
            JwtTokenUseClaimNames.Access,
            StringComparison.Ordinal);
}
