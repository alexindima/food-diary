using System.Security.Claims;
using FoodDiary.Presentation.Api.Extensions;
using Microsoft.AspNetCore.SignalR;

namespace FoodDiary.Presentation.Api.Services;

public sealed class UserIdProvider : IUserIdProvider {
    public string? GetUserId(HubConnectionContext? connection) {
        ClaimsPrincipal? user = connection?.User;
        return user?.GetUserGuid()?.ToString();
    }
}
