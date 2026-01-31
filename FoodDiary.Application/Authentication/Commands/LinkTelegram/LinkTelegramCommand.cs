using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Contracts.Users;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Authentication.Commands.LinkTelegram;

public sealed record LinkTelegramCommand(UserId UserId, string InitData) : ICommand<Result<UserResponse>>;
