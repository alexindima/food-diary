using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Abstractions.Authentication.Services;

public interface IAuthenticationTokenService {
    Task<IssuedAuthenticationTokens> IssueAndStoreAsync(User user, CancellationToken cancellationToken);
    string IssueAccessToken(User user);
}

public sealed record IssuedAuthenticationTokens(string AccessToken, string RefreshToken);
