using FoodDiary.Mediator;
using FoodDiary.Modules.Billing.Contracts.Models;

namespace FoodDiary.Modules.Billing.Contracts.Commands.SendCancellationConfirmations;

public sealed record SendCancellationConfirmationsCommand(int BatchSize) : IRequest<BillingCancellationEmailRunResult>;
