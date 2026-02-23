using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Common.Interfaces.Services;

public interface IEmailVerificationNotifier
{
    Task NotifyEmailVerifiedAsync(UserId userId, CancellationToken cancellationToken = default);
}
