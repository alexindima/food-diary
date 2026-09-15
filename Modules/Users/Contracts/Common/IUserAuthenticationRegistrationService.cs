using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Users.Contracts.Common;

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
