using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Identity.Application.Authentication.Models;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.AdminSsoStart;

public sealed record AdminSsoStartCommand(Guid UserId) : ICommand<Result<AdminSsoStartModel>>;
