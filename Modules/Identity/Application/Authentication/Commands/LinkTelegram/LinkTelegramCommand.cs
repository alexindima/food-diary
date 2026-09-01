using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Models;

namespace FoodDiary.Application.Identity.Authentication.Commands.LinkTelegram;

public sealed record LinkTelegramCommand(Guid UserId, string InitData) : ICommand<Result<UserModel>>;
