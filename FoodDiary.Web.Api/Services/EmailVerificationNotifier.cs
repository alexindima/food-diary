using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.WebApi.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace FoodDiary.WebApi.Services;

public sealed class EmailVerificationNotifier(IHubContext<EmailVerificationHub> hubContext)
    : IEmailVerificationNotifier
{
    public Task NotifyEmailVerifiedAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        return hubContext.Clients.User(userId.Value.ToString())
            .SendAsync("EmailVerified", cancellationToken);
    }
}
