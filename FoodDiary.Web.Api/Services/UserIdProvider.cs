using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace FoodDiary.WebApi.Services;

public sealed class UserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        var user = connection.User;
        if (user is null)
        {
            return null;
        }

        return user.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? user.FindFirstValue("nameid")
               ?? user.FindFirstValue("sub");
    }
}
