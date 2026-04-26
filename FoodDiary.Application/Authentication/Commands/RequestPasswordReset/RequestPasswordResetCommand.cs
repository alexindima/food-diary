using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;

namespace FoodDiary.Application.Authentication.Commands.RequestPasswordReset;

public record RequestPasswordResetCommand(string Email, string? ClientOrigin = null) : ICommand<Result>;
