using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Identity.Authentication.Models;

namespace FoodDiary.Application.Identity.Authentication.Commands.AdminSsoStart;

public sealed record AdminSsoStartCommand(Guid UserId) : ICommand<Result<AdminSsoStartModel>>;
