using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;

namespace FoodDiary.Application.Authentication.Commands.VerifyEmail;

public record VerifyEmailCommand(
    Guid UserId,
    string Token) : ICommand<Result>;
