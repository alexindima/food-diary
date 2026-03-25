using FoodDiary.Presentation.Api.Extensions;
using Microsoft.AspNetCore.SignalR;

namespace FoodDiary.Presentation.Api.Services;

public sealed class UserIdProvider : IUserIdProvider {
    public string? GetUserId(HubConnectionContext connection) {
        var user = connection.User;
        if (user is null) {
            return null;
        }

        return user.GetUserGuid()?.ToString();
    }
}
