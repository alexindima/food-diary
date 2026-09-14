using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FoodDiary.Modules.Ai.Presentation.Hubs;

[Authorize]
public sealed class FoodRecognitionHub : Hub;
