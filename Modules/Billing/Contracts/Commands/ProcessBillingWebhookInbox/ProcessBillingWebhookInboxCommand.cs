using FoodDiary.Mediator;
using FoodDiary.Modules.Billing.Contracts.Models;

namespace FoodDiary.Modules.Billing.Contracts.Commands.ProcessBillingWebhookInbox;

// Each inbox item owns its transaction and failure bookkeeping.
public sealed record ProcessBillingWebhookInboxCommand(int BatchSize) : IRequest<BillingWebhookInboxRunResult>;
