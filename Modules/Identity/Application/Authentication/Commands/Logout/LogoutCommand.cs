using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.Identity.Authentication.Commands.Logout;

public sealed record LogoutCommand(string? RefreshToken) : ICommand<Result>;
