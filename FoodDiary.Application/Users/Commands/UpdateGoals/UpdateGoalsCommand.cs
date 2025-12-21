using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Contracts.Goals;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Users.Commands.UpdateGoals;

public record UpdateGoalsCommand(
    UserId? UserId,
    double? DailyCalorieTarget,
    double? ProteinTarget,
    double? FatTarget,
    double? CarbTarget,
    double? FiberTarget,
    double? WaterGoal,
    double? DesiredWeight,
    double? DesiredWaist
) : ICommand<Result<GoalsResponse>>;
