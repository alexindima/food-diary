using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Users.Models;

namespace FoodDiary.Application.Users.Commands.UpdateUser;

public record UpdateUserCommand(
    Guid? UserId,
    string? Username,
    string? FirstName,
    string? LastName,
    DateTime? BirthDate,
    string? Gender,
    double? Weight,
    double? Height,
    string? ActivityLevel,
    int? StepGoal,
    double? HydrationGoal,
    string? Language,
    string? ProfileImage,
    Guid? ProfileImageAssetId,
    DashboardLayoutModel? DashboardLayout,
    bool? IsActive
) : ICommand<Result<UserModel>>;
