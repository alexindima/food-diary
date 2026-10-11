using FoodDiary.Mediator;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Users.Contracts.Commands.InitializePendingBodyGoals;

// Trusted owner composition: stage the goal start in the measurement caller's unit of work.
public sealed record InitializePendingBodyGoalsCommand(
    UserId UserId,
    MeasuredWeightKg? Weight = null,
    MeasuredWaistCm? Waist = null) : IRequest<bool>;
