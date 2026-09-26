using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.LinkGoogle;

public sealed record LinkGoogleCommand(Guid UserId, string Credential) : ICommand<Result<UserModel>>;
