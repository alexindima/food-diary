using System.Security.Claims;
using FoodDiary.Application.Abstractions.Authentication.Abstractions;

namespace FoodDiary.Web.Api.Extensions;

internal static class JwtTokenUseValidator {
    internal static bool IsAccessToken(ClaimsPrincipal? principal) =>
        string.Equals(
            principal?.FindFirst(JwtTokenUseClaimNames.ClaimType)?.Value,
            JwtTokenUseClaimNames.Access,
            StringComparison.Ordinal);
}
