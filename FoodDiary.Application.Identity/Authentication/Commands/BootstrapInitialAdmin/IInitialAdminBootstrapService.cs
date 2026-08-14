using FoodDiary.Results;

namespace FoodDiary.Application.Identity.Authentication.Commands.BootstrapInitialAdmin;

public interface IInitialAdminBootstrapService {
    Task<Result<BootstrapInitialAdminModel>> BootstrapAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);
}
