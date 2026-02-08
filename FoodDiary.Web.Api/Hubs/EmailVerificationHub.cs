using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FoodDiary.WebApi.Hubs;

[Authorize]
public sealed class EmailVerificationHub : Hub
{
}
