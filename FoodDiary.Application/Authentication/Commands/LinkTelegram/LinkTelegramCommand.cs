using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Users.Models;

namespace FoodDiary.Application.Authentication.Commands.LinkTelegram;

public sealed record LinkTelegramCommand(Guid UserId, string InitData) : ICommand<Result<UserModel>>;
