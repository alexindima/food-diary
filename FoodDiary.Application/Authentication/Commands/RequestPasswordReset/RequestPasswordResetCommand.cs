using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;

namespace FoodDiary.Application.Authentication.Commands.RequestPasswordReset;

public record RequestPasswordResetCommand(string Email) : ICommand<Result<bool>>;
