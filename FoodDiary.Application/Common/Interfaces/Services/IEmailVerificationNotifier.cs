using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Common.Interfaces.Services;

public interface IEmailVerificationNotifier
{
    Task NotifyEmailVerifiedAsync(UserId userId, CancellationToken cancellationToken = default);
}
