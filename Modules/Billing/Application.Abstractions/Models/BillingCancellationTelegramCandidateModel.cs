using FoodDiary.Modules.Billing.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Billing.Application.Abstractions.Models;

public sealed record BillingCancellationTelegramCandidateModel(BillingCancellationId Id, UserId UserId);
