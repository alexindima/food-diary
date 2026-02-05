using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Contracts.Authentication;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Authentication.Commands.AdminSsoStart;

public sealed record AdminSsoStartCommand(UserId UserId) : ICommand<Result<AdminSsoStartResponse>>;
