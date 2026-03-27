using FoodDiary.Application.Authentication.Common;
using FoodDiary.Presentation.Api.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace FoodDiary.Presentation.Api.Services;

public sealed class EmailVerificationNotifier(IHubContext<EmailVerificationHub> hubContext)
    : IEmailVerificationNotifier {
    public Task NotifyEmailVerifiedAsync(Guid userId, CancellationToken cancellationToken = default) {
        return hubContext.Clients.User(userId.ToString())
            .SendAsync(EmailVerificationHubMethods.EmailVerified, cancellationToken);
    }
}
