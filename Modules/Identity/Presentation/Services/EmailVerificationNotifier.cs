using FoodDiary.Modules.Identity.Presentation.Hubs;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using Microsoft.AspNetCore.SignalR;

namespace FoodDiary.Modules.Identity.Presentation.Services;

public sealed class EmailVerificationNotifier(IHubContext<EmailVerificationHub> hubContext)
    : IEmailVerificationNotifier {
    public Task NotifyEmailVerifiedAsync(Guid userId, CancellationToken cancellationToken = default) {
        return hubContext.Clients.User(userId.ToString())
            .SendAsync(EmailVerificationHubMethods.EmailVerified, cancellationToken);
    }
}
