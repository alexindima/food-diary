using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FoodDiary.Presentation.Api.Hubs;

[Authorize]
public sealed class EmailVerificationHub : Hub {
}
