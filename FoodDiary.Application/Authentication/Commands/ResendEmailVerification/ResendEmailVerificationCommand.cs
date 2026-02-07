using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Authentication.Commands.ResendEmailVerification;

public record ResendEmailVerificationCommand(UserId UserId) : ICommand<Result<bool>>;
