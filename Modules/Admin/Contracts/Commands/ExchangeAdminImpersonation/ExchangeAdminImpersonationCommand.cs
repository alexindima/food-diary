using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.Admin.Commands.ExchangeAdminImpersonation;

public sealed record ExchangeAdminImpersonationCommand(string Code) : ICommand<Result<string>>;
