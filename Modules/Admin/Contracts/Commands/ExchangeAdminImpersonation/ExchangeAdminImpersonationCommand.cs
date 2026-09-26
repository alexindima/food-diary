using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Admin.Contracts.Commands.ExchangeAdminImpersonation;

public sealed record ExchangeAdminImpersonationCommand(string Code) : ICommand<Result<string>>;
