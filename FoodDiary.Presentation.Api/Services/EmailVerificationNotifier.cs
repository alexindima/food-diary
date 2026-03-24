using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Presentation.Api.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace FoodDiary.Presentation.Api.Services;

public sealed class EmailVerificationNotifier(IHubContext<EmailVerificationHub> hubContext)
    : IEmailVerificationNotifier {
    public Task NotifyEmailVerifiedAsync(UserId userId, CancellationToken cancellationToken = default) {
        return hubContext.Clients.User(userId.Value.ToString())
            .SendAsync("EmailVerified", cancellationToken);
    }
}
