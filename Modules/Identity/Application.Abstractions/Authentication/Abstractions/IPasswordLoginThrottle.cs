using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Models;

namespace FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;

public interface IPasswordLoginThrottle {
    Task<PasswordLoginAttempt> TryBeginAsync(string email, CancellationToken cancellationToken = default);

    Task<bool> CompleteAsync(
        PasswordLoginAttempt attempt,
        PasswordLoginAttemptOutcome outcome,
        CancellationToken cancellationToken = default);
}
