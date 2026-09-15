using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Application.Abstractions.Authentication.Services;
using FoodDiary.Application.Abstractions.Authentication.Models;

namespace FoodDiary.Modules.Identity.Infrastructure.Authentication;

public sealed class ImpersonationTokenIssuer(IJwtTokenGenerator tokenGenerator) : IImpersonationTokenIssuer {
    public string IssueAccessToken(ImpersonationTokenRequest request) {
        return tokenGenerator.GenerateAccessToken(
            request.SubjectId,
            request.Email,
            request.Roles,
            new JwtImpersonationContext(request.ActorId, request.Reason),
            request.SecurityVersion);
    }
}
