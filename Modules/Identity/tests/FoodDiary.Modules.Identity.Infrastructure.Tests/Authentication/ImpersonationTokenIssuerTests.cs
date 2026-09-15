using FoodDiary.Application.Abstractions.Options;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FoodDiary.Modules.Identity.Contracts.Authentication.Models;
using FoodDiary.Modules.Identity.Contracts.Authentication.Services;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FoodDiary.Modules.Identity.Infrastructure.Tests.Authentication;

[ExcludeFromCodeCoverage]
public sealed class ImpersonationTokenIssuerTests {
    [Theory]
    [InlineData("target@example.com")]
    [InlineData(null)]
    public void IssueAccessToken_PreservesImpersonationIdentityAndAccessOnlySemantics(string? email) {
        var services = new ServiceCollection();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IOptions<JwtOptions>>(Microsoft.Extensions.Options.Options.Create(new JwtOptions {
            SecretKey = "impersonation-adapter-test-key-only-123456789",
            Issuer = "FoodDiaryTests",
            Audience = "FoodDiaryTestClients",
            ExpirationMinutes = 60,
            RefreshTokenExpirationDays = 7,
            RememberMeRefreshTokenExpirationDays = 90,
        }));
        services.AddIdentityAuthenticationInfrastructure();
        using ServiceProvider provider = services.BuildServiceProvider();
        IImpersonationTokenIssuer issuer = provider.GetRequiredService<IImpersonationTokenIssuer>();
        var subjectId = UserId.New();
        var actorId = UserId.New();
        string token = issuer.IssueAccessToken(new ImpersonationTokenRequest(
            subjectId, email, ["User", "Premium"], actorId, "Support request", 42));
        JwtSecurityToken parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Multiple(
            () => Assert.Same(issuer, provider.GetRequiredService<IImpersonationTokenIssuer>()),
            () => Assert.Equal(subjectId.Value.ToString(), ClaimValue(parsed, ClaimTypes.NameIdentifier)),
            () => Assert.Equal(actorId.Value.ToString(), ClaimValue(parsed, JwtImpersonationClaimNames.ActorUserId)),
            () => Assert.Equal("Support request", ClaimValue(parsed, JwtImpersonationClaimNames.Reason)),
            () => Assert.Equal("true", ClaimValue(parsed, JwtImpersonationClaimNames.IsImpersonation)),
            () => Assert.Equal("42", ClaimValue(parsed, JwtSecurityClaimNames.SecurityVersion)),
            () => Assert.Equal(JwtTokenUseClaimNames.Access, ClaimValue(parsed, JwtTokenUseClaimNames.ClaimType)),
            () => Assert.Equal(email, ClaimValue(parsed, ClaimTypes.Email)),
            () => Assert.Equal(["Premium", "User"], parsed.Claims.Where(claim => string.Equals(claim.Type, ClaimTypes.Role, StringComparison.Ordinal))
                .Select(claim => claim.Value).Order(StringComparer.Ordinal), StringComparer.Ordinal),
            () => Assert.Null(provider.GetRequiredService<IJwtTokenGenerator>().ValidateToken(token)));
    }

    private static string? ClaimValue(JwtSecurityToken token, string type) =>
        token.Claims.SingleOrDefault(claim => string.Equals(claim.Type, type, StringComparison.Ordinal))?.Value;
}
