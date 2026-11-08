using FoodDiary.Mediator;
using FoodDiary.Modules.Billing.Contracts.Models;

namespace FoodDiary.Modules.Billing.Contracts.Commands.SendTelegramCancellationConfirmations;

public sealed record SendTelegramCancellationConfirmationsCommand(int BatchSize) : IRequest<BillingCancellationTelegramRunResult>;
