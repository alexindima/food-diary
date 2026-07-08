using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Authentication.Models;

namespace FoodDiary.Application.Authentication.Commands.AdminSsoStart;

public sealed record AdminSsoStartCommand(Guid UserId) : ICommand<Result<AdminSsoStartModel>>;
