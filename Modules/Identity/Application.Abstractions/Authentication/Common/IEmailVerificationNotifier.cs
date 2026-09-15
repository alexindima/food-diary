namespace FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;

public interface IEmailVerificationNotifier {
    Task NotifyEmailVerifiedAsync(Guid userId, CancellationToken cancellationToken = default);
}
