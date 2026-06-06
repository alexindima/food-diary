using FoodDiary.Application.Authentication.Common;

namespace FoodDiary.Initializer;

internal sealed class NoOpEmailVerificationNotifier : IEmailVerificationNotifier {
    public Task NotifyEmailVerifiedAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
