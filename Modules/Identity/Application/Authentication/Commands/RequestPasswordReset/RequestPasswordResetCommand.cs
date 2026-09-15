using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.RequestPasswordReset;

public record RequestPasswordResetCommand(string Email, string? ClientOrigin = null) : ICommand<Result>;
