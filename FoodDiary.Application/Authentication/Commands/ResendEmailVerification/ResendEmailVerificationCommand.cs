using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;

namespace FoodDiary.Application.Authentication.Commands.ResendEmailVerification;

public record ResendEmailVerificationCommand(Guid UserId) : ICommand<Result<bool>>;
