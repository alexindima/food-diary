using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Users.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Authentication.Commands.LinkTelegram;

public sealed record LinkTelegramCommand(UserId UserId, string InitData) : ICommand<Result<UserModel>>;
