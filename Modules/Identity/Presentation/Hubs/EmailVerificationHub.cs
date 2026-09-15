using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FoodDiary.Modules.Identity.Presentation.Hubs;

[Authorize]
public sealed class EmailVerificationHub : Hub;
