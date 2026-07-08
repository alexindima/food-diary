using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.Authentication.Commands.ResendEmailVerification;

public record ResendEmailVerificationCommand(Guid UserId, string? ClientOrigin = null) : ICommand<Result>;
