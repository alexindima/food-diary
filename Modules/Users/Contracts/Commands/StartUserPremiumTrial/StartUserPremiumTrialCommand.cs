using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;
using FoodDiary.Mediator;

namespace FoodDiary.Application.Abstractions.Commands.StartUserPremiumTrial;

// Executes within the caller-owned unit of work; this request does not commit.
public sealed record StartUserPremiumTrialCommand(
    UserId UserId,
    DateTime StartedAtUtc,
    TimeSpan Duration) : IRequest<Result<UserBillingProfileModel>>;
