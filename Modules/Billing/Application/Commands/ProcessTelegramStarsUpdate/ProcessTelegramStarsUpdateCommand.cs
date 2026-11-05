using FoodDiary.Mediator;
using FoodDiary.Modules.Billing.Application.Abstractions.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Billing.Application.Commands.ProcessTelegramStarsUpdate;

// Owns the per-user transaction, including the update receipt and financial/access writes.
public sealed record ProcessTelegramStarsUpdateCommand(TelegramStarsUpdateModel Update)
    : IRequest<Result<TelegramStarsUpdateResultModel>>;
