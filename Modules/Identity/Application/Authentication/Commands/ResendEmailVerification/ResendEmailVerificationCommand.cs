using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.ResendEmailVerification;

public record ResendEmailVerificationCommand(Guid UserId, string? ClientOrigin = null) : ICommand<Result>;
