using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.Logout;

public sealed record LogoutCommand(string? RefreshToken) : ICommand<Result>;
