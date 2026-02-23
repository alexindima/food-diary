using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Web.Api.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace FoodDiary.Web.Api.Services;

public sealed class EmailVerificationNotifier(IHubContext<EmailVerificationHub> hubContext)
    : IEmailVerificationNotifier {
    public Task NotifyEmailVerifiedAsync(UserId userId, CancellationToken cancellationToken = default) {
        return hubContext.Clients.User(userId.Value.ToString())
            .SendAsync("EmailVerified", cancellationToken);
    }
}
