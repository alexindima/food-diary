using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Domain.Entities.Billing;

namespace FoodDiary.Application.Billing.Commands.ProcessBillingWebhook;

public sealed record BillingWebhookProcessingContext(BillingSubscription? Subscription, UserBillingProfileModel User);
