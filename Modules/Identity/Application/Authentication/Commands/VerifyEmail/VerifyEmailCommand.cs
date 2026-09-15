using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.VerifyEmail;

public record VerifyEmailCommand(
    Guid UserId,
    string Token) : ICommand<Result>;
