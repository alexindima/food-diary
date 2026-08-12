using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.Abstractions.Users.Common;

public interface IUserAuthenticationRegistrationService {
    Task<Result<UserAuthenticationPrincipalModel>> RegisterAsync(
        UserRegistrationModel registration,
        CancellationToken cancellationToken = default);

    Task<UserInitialAdminBootstrapModel> BootstrapInitialAdminAsync(
        string email,
        string password,
        IReadOnlyCollection<string> roles,
        CancellationToken cancellationToken = default);
}
