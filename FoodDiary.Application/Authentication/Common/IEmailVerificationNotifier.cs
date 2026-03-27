namespace FoodDiary.Application.Authentication.Common;

public interface IEmailVerificationNotifier {
    Task NotifyEmailVerifiedAsync(Guid userId, CancellationToken cancellationToken = default);
}
