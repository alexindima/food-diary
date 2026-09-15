using FoodDiary.Modules.Identity.Contracts.Authentication.Models;

namespace FoodDiary.Modules.Identity.Contracts.Authentication.Services;

public interface IImpersonationTokenIssuer {
    string IssueAccessToken(ImpersonationTokenRequest request);
}
