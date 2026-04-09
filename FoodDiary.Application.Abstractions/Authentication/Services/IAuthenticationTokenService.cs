using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Authentication.Services;

public interface IAuthenticationTokenService {
    Task<IssuedAuthenticationTokens> IssueAndStoreAsync(User user, CancellationToken cancellationToken);
    string IssueAccessToken(User user);
}

public sealed record IssuedAuthenticationTokens(string AccessToken, string RefreshToken);
