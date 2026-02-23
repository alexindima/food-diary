using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FoodDiary.Web.Api.Hubs;

[Authorize]
public sealed class EmailVerificationHub : Hub {
}
