using FoodDiary.Application.Abstractions.Authentication.Models;

namespace FoodDiary.Application.Abstractions.Authentication.Services;

public interface IImpersonationTokenIssuer {
    string IssueAccessToken(ImpersonationTokenRequest request);
}
