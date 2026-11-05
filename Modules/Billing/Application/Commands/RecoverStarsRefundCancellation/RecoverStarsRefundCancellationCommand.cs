using FoodDiary.Mediator;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Billing.Application.Commands.RecoverStarsRefundCancellation;

public sealed record RecoverStarsRefundCancellationCommand(UserId UserId) : IRequest<Result<Unit>>;
