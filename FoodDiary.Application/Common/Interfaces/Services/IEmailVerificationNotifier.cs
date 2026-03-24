namespace FoodDiary.Application.Common.Interfaces.Services;

public interface IEmailVerificationNotifier {
    Task NotifyEmailVerifiedAsync(Guid userId, CancellationToken cancellationToken = default);
}
