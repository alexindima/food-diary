using FoodDiary.Mediator;
using FoodDiary.Modules.Billing.Application.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Billing.Application.Commands.CancelSubscription;

// Explicit transaction ownership keeps Paddle HTTP outside replayable database callbacks.
public sealed record CancelSubscriptionCommand(Guid? UserId) : IRequest<Result<BillingCancellationModel>>;
