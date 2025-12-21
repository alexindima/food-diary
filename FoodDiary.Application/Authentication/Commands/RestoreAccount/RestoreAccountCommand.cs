using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Contracts.Authentication;

namespace FoodDiary.Application.Authentication.Commands.RestoreAccount;

public record RestoreAccountCommand(
    string Email,
    string Password
) : ICommand<Result<AuthenticationResponse>>;
