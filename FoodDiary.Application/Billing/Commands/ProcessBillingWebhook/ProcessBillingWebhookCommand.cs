using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.Billing.Commands.ProcessBillingWebhook;

public sealed record ProcessBillingWebhookCommand(string Provider, string Payload, string SignatureHeader) : ICommand<Result>;
