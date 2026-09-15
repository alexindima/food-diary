using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Models;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.LinkTelegram;

public sealed record LinkTelegramCommand(Guid UserId, string InitData) : ICommand<Result<UserModel>>;
