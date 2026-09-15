using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.Users.Contracts.Commands.StartUserPremiumTrial;

// Executes within the caller-owned unit of work; this request does not commit.
public sealed record StartUserPremiumTrialCommand(
    UserId UserId,
    DateTime StartedAtUtc,
    TimeSpan Duration) : IRequest<Result<UserBillingProfileModel>>;
