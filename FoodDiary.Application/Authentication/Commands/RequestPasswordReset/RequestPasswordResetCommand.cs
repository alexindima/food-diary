using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.Authentication.Commands.RequestPasswordReset;

public record RequestPasswordResetCommand(string Email, string? ClientOrigin = null) : ICommand<Result>;
