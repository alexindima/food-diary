using FoodDiary.Mediator;
using FoodDiary.Modules.Billing.Contracts.Models;

namespace FoodDiary.Modules.Billing.Contracts.Commands.RenewDueSubscriptions;

// The handler owns short transactions around persisted results; provider calls are outside them.
public sealed record RenewDueSubscriptionsCommand(string Provider, int BatchSize) : IRequest<BillingRenewalRunResult>;
