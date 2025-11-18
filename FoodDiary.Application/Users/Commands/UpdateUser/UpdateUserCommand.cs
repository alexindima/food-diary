using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Contracts.Users;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Users.Commands.UpdateUser;

public record UpdateUserCommand(
    UserId? UserId,
    string? Username,
    string? FirstName,
    string? LastName,
    DateTime? BirthDate,
    string? Gender,
    double? Weight,
    double? Height,
    string? ActivityLevel,
    double? DailyCalorieTarget,
    double? ProteinTarget,
    double? FatTarget,
    double? CarbTarget,
    int? StepGoal,
    double? WaterGoal,
    string? ProfileImage,
    bool? IsActive
) : ICommand<Result<UserResponse>>;
