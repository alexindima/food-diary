using FoodDiary.Application.Abstractions.Authentication.Common;

namespace FoodDiary.JobManager.Services;

internal sealed class NoOpEmailVerificationNotifier : IEmailVerificationNotifier {
    public Task NotifyEmailVerifiedAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
