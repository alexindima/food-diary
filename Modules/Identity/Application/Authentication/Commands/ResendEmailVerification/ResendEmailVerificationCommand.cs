using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.Identity.Authentication.Commands.ResendEmailVerification;

public record ResendEmailVerificationCommand(Guid UserId, string? ClientOrigin = null) : ICommand<Result>;
