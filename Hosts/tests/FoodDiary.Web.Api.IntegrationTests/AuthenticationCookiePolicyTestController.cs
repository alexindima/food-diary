using Asp.Versioning;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Responses;
using FoodDiary.Modules.Identity.Presentation.Filters;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Web.Api.IntegrationTests;

// Only TransportApiWebApplicationFactory adds this test assembly to MVC.
[ApiController]
[ApiVersionNeutral]
[AllowAnonymous]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("api/v1/test-auth-cookie")]
[ExcludeFromCodeCoverage]
public sealed class AuthenticationCookiePolicyTestController(IJwtTokenGenerator tokens) : ControllerBase {
    [HttpPost("main")]
    public IActionResult MainAuthentication() => Ok(new AuthenticationHttpResponse("test-access",
        tokens.GenerateRefreshToken(UserId.New(), email: null, [], refreshSessionId: null), User: null!));

    [HttpPost("independent-bearer")]
    [SuppressRefreshTokenCookie]
    public IActionResult IndependentBearer() => Ok(new AuthenticationHttpResponse("test-access", "test-refresh", User: null!));
}
