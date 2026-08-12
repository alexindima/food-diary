using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.Authentication.Commands.LinkGoogle;

public sealed record LinkGoogleCommand(Guid UserId, string Credential) : ICommand<Result<UserModel>>;
